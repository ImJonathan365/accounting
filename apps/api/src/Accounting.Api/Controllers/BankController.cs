using Accounting.Api.Filters;
using Accounting.Api.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
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
    private readonly IValidator<CreateBankAccountDto>      _createAccountValidator;
    private readonly IValidator<ImportBankTransactionDto>  _importValidator;

    public BankController(
        IBankService service,
        IValidator<CreateBankAccountDto> createAccountValidator,
        IValidator<ImportBankTransactionDto> importValidator)
    {
        _service               = service;
        _createAccountValidator = createAccountValidator;
        _importValidator        = importValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<BankAccountDto>>> GetAll(Guid orgId, CancellationToken ct) =>
        Ok(await _service.GetAccountsAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create(Guid orgId, [FromBody] CreateBankAccountDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        await _createAccountValidator.ValidateAndThrowAsync(dto, ct);
        var result = await _service.CreateAccountAsync(orgId, dto, ct);
        return Ok(result);
    }

    [HttpGet("{bankAccountId:guid}/reconciliation")]
    public async Task<ActionResult<BankReconciliationDto>> GetReconciliation(Guid orgId, Guid bankAccountId, CancellationToken ct) =>
        Ok(await _service.GetReconciliationAsync(orgId, bankAccountId, ct));

    [HttpPost("{bankAccountId:guid}/transactions/import")]
    public async Task<ActionResult<List<BankTransactionDto>>> Import(
        Guid orgId, Guid bankAccountId, [FromBody] List<ImportBankTransactionDto> rows, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        foreach (var row in rows)
            await _importValidator.ValidateAndThrowAsync(row, ct);
        return Ok(await _service.ImportTransactionsAsync(orgId, bankAccountId, rows, ct));
    }

    [HttpPatch("{bankAccountId:guid}/transactions/{txId:guid}/match")]
    public async Task<ActionResult<BankTransactionDto>> Match(
        Guid orgId, Guid bankAccountId, Guid txId, [FromBody] MatchTransactionDto dto, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        return Ok(await _service.MatchTransactionAsync(orgId, txId, dto, ct));
    }

    [HttpPatch("{bankAccountId:guid}/transactions/{txId:guid}/exclude")]
    public async Task<ActionResult<BankTransactionDto>> Exclude(Guid orgId, Guid bankAccountId, Guid txId, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        return Ok(await _service.ExcludeTransactionAsync(orgId, txId, ct));
    }

    [HttpPatch("{bankAccountId:guid}/transactions/{txId:guid}/unmatch")]
    public async Task<ActionResult<BankTransactionDto>> Unmatch(Guid orgId, Guid bankAccountId, Guid txId, CancellationToken ct)
    {
        if (!OrgAuth.HasRole(HttpContext, "owner", "admin")) return Forbid();
        return Ok(await _service.UnmatchTransactionAsync(orgId, txId, ct));
    }
}
