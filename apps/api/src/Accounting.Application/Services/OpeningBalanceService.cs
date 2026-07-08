using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IOpeningBalanceService
{
    Task<OpeningBalanceResultDto> SetAsync(Guid orgId, SetOpeningBalancesRequest dto, CancellationToken ct = default);
}

public class OpeningBalanceService : IOpeningBalanceService
{
    private readonly IJournalRepository  _journal;
    private readonly IAccountRepository  _accounts;

    public OpeningBalanceService(IJournalRepository journal, IAccountRepository accounts)
    {
        _journal  = journal;
        _accounts = accounts;
    }

    public async Task<OpeningBalanceResultDto> SetAsync(Guid orgId, SetOpeningBalancesRequest dto, CancellationToken ct = default)
    {
        if (!DateOnly.TryParse(dto.Date, out var date))
            throw new ArgumentException("Fecha inválida.");

        var nonZeroLines = dto.Lines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();
        if (nonZeroLines.Count == 0)
            throw new InvalidOperationException("Debes ingresar al menos una línea con valor.");

        var totalDebit  = nonZeroLines.Sum(l => l.Debit);
        var totalCredit = nonZeroLines.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) >= 0.01m)
            throw new InvalidOperationException(
                $"El asiento no balancea: débitos {totalDebit:N2} ≠ créditos {totalCredit:N2}.");

        // Validate all accounts belong to the org
        var accountIds = nonZeroLines.Select(l => l.AccountId).ToHashSet();
        var orgAccounts = await _accounts.GetByOrganizationAsync(orgId, ct);
        var validIds    = orgAccounts.Select(a => a.Id).ToHashSet();
        var invalid     = accountIds.Except(validIds).ToList();
        if (invalid.Count > 0)
            throw new InvalidOperationException("Una o más cuentas no pertenecen a la organización.");

        var entry = new JournalEntry
        {
            OrganizationId = orgId,
            Date           = date,
            Description    = dto.Description?.Trim() is { Length: > 0 } d ? d : "Saldos iniciales",
            Status         = JournalStatus.Posted,
            Lines          = nonZeroLines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Debit     = l.Debit,
                Credit    = l.Credit,
            }).ToList(),
        };

        await _journal.AddAsync(entry, ct);
        await _journal.SaveChangesAsync(ct);

        return new OpeningBalanceResultDto(entry.Id, date.ToString("yyyy-MM-dd"), totalDebit, totalCredit);
    }
}
