using System.Security.Claims;
using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/bank-accounts")]
public class BankController : ControllerBase
{
    private readonly IBankService                          _service;
    private readonly IAuditService                         _audit;
    private readonly IValidator<CreateBankAccountDto>      _createAccountValidator;
    private readonly IValidator<ImportBankTransactionDto>  _importValidator;

    public BankController(
        IBankService service,
        IAuditService audit,
        IValidator<CreateBankAccountDto> createAccountValidator,
        IValidator<ImportBankTransactionDto> importValidator)
    {
        _service               = service;
        _audit                 = audit;
        _createAccountValidator = createAccountValidator;
        _importValidator        = importValidator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<BankAccountDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAccountsAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create(Guid orgId, [FromBody] CreateBankAccountDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createAccountValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAccountAsync(orgId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BankAccountCreated, "BankAccount", result.Id,
            $"Creó cuenta bancaria \"{result.Name}\"", ct);
        return Ok(result);
    }

    [HttpGet("{bankAccountId:guid}/reconciliation")]
    public async Task<ActionResult<BankReconciliationDto>> GetReconciliation(
        Guid orgId, Guid bankAccountId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        return Ok(await _service.GetReconciliationAsync(orgId, bankAccountId, page, pageSize, ct));
    }

    [HttpPost("{bankAccountId:guid}/transactions/import")]
    public async Task<ActionResult<List<BankTransactionDto>>> Import(
        Guid orgId, Guid bankAccountId, [FromBody] List<ImportBankTransactionDto> rows, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        foreach (var row in rows)
            await _importValidator.ValidateAndThrowAsync(row, ct);
        var result = await _service.ImportTransactionsAsync(orgId, bankAccountId, rows, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BankImported, "BankAccount", bankAccountId,
            $"Importó {result.Count} transacciones vía JSON", ct);
        return Ok(result);
    }

    [HttpPost("{bankAccountId:guid}/transactions/import-csv")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<List<BankTransactionDto>>> ImportCsv(
        Guid orgId, Guid bankAccountId, IFormFile file, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();

        if (file is null || file.Length == 0)
            throw new ArgumentException("El archivo CSV está vacío.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El archivo debe ser de tipo CSV.");

        List<ImportBankTransactionDto> rows;
        using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
            rows = ParseCsv(await reader.ReadToEndAsync(ct));

        if (rows.Count == 0)
            throw new ArgumentException("El archivo CSV no contiene transacciones.");

        foreach (var row in rows)
            await _importValidator.ValidateAndThrowAsync(row, ct);

        var result = await _service.ImportTransactionsAsync(orgId, bankAccountId, rows, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BankImported, "BankAccount", bankAccountId,
            $"Importó {result.Count} transacciones desde CSV", ct);
        return Ok(result);
    }

    // Parses a CSV with header row: Date,Description,Amount,Type
    // Supports quoted fields (commas inside descriptions).
    private static List<ImportBankTransactionDto> ParseCsv(string content)
    {
        var rows  = new List<ImportBankTransactionDto>();
        var lines = content.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header
        foreach (var line in lines.Skip(1))
        {
            var fields = SplitCsvLine(line);
            if (fields.Count < 3) continue;

            var date        = fields[0].Trim();
            var description = fields[1].Trim();
            var amountStr   = fields[2].Trim();
            var type        = fields.Count >= 4 ? fields[3].Trim() : "";

            if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount))
                throw new ArgumentException($"Monto inválido en la línea: \"{line}\".");

            rows.Add(new ImportBankTransactionDto(date, description, amount, type));
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields  = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    [HttpPatch("{bankAccountId:guid}/transactions/{txId:guid}/match")]
    public async Task<ActionResult<BankTransactionDto>> Match(
        Guid orgId, Guid bankAccountId, Guid txId, [FromBody] MatchTransactionDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        var result = await _service.MatchTransactionAsync(orgId, txId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BankTxMatched, "BankTransaction", txId,
            $"Concilió transacción bancaria con asiento {dto.JournalEntryId}", ct);
        return Ok(result);
    }

    [HttpPatch("{bankAccountId:guid}/transactions/{txId:guid}/exclude")]
    public async Task<ActionResult<BankTransactionDto>> Exclude(Guid orgId, Guid bankAccountId, Guid txId, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        var result = await _service.ExcludeTransactionAsync(orgId, txId, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BankTxExcluded, "BankTransaction", txId, null, ct);
        return Ok(result);
    }

    [HttpPatch("{bankAccountId:guid}/transactions/{txId:guid}/unmatch")]
    public async Task<ActionResult<BankTransactionDto>> Unmatch(Guid orgId, Guid bankAccountId, Guid txId, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        var result = await _service.UnmatchTransactionAsync(orgId, txId, ct);
        await _audit.LogAsync(orgId, CurrentUserId, AuditActions.BankTxUnmatched, "BankTransaction", txId, null, ct);
        return Ok(result);
    }
}
