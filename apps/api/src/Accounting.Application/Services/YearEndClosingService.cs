using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IYearEndClosingService
{
    Task<YearEndStatusDto>  GetStatusAsync(Guid orgId, int year, CancellationToken ct = default);
    Task<YearEndStatusDto>  CloseYearAsync(Guid orgId, Guid userId, YearEndCloseRequestDto dto, CancellationToken ct = default);
}

public class YearEndClosingService : IYearEndClosingService
{
    private readonly IYearEndClosingRepository  _closings;
    private readonly IAccountRepository         _accounts;
    private readonly IReportRepository          _reports;
    private readonly IJournalRepository         _journal;
    private readonly IAccountingPeriodRepository _periods;

    public YearEndClosingService(
        IYearEndClosingRepository closings,
        IAccountRepository accounts,
        IReportRepository reports,
        IJournalRepository journal,
        IAccountingPeriodRepository periods)
    {
        _closings = closings;
        _accounts = accounts;
        _reports  = reports;
        _journal  = journal;
        _periods  = periods;
    }

    public async Task<YearEndStatusDto> GetStatusAsync(Guid orgId, int year, CancellationToken ct = default)
    {
        var closing = await _closings.GetAsync(orgId, year, ct);
        return ToDto(year, closing);
    }

    public async Task<YearEndStatusDto> CloseYearAsync(
        Guid orgId, Guid userId, YearEndCloseRequestDto dto, CancellationToken ct = default)
    {
        var year = dto.Year;

        if (year < 2000 || year > DateTime.UtcNow.Year)
            throw new ArgumentException("El año debe ser un período ya transcurrido.");

        var existing = await _closings.GetAsync(orgId, year, ct);
        if (existing is not null)
            throw new InvalidOperationException($"El año {year} ya fue cerrado.");

        for (var month = 1; month <= 12; month++)
        {
            if (!await _periods.IsClosedAsync(orgId, year, month, ct))
            {
                var monthName = new DateOnly(year, month, 1).ToString("MMMM");
                throw new InvalidOperationException(
                    $"Debes cerrar todos los períodos de {year} antes del cierre de ejercicio. El período de {monthName} está abierto.");
            }
        }

        var retainedAccount = await _accounts.GetByIdAsync(dto.RetainedEarningsAccountId, orgId, ct)
            ?? throw new KeyNotFoundException("La cuenta de resultados acumulados no existe.");

        if (retainedAccount.Type != AccountType.Equity)
            throw new ArgumentException("La cuenta de resultados acumulados debe ser de tipo Capital (Equity).");

        var from = new DateOnly(year, 1, 1);
        var to   = new DateOnly(year, 12, 31);
        var allAccounts  = await _accounts.GetByOrganizationAsync(orgId, ct);
        var periodBals   = await _reports.GetAccountBalancesAsync(orgId, from, to, ct);
        var balanceMap   = periodBals.ToDictionary(b => b.AccountId);
        var accountMap   = allAccounts.ToDictionary(a => a.Id);

        var lines = new List<JournalLine>();

        decimal netIncome = 0m;

        // Close income accounts (debit them to zero out)
        foreach (var acc in allAccounts.Where(a => a.Type == AccountType.Income))
        {
            if (!balanceMap.TryGetValue(acc.Id, out var bal)) continue;
            var balance = bal.TotalCredit - bal.TotalDebit;
            if (balance == 0m) continue;
            lines.Add(new JournalLine { AccountId = acc.Id, Debit = balance, Credit = 0 });
            netIncome += balance;
        }

        // Close expense accounts (credit them to zero out)
        foreach (var acc in allAccounts.Where(a => a.Type == AccountType.Expense))
        {
            if (!balanceMap.TryGetValue(acc.Id, out var bal)) continue;
            var balance = bal.TotalDebit - bal.TotalCredit;
            if (balance == 0m) continue;
            lines.Add(new JournalLine { AccountId = acc.Id, Debit = 0, Credit = balance });
            netIncome -= balance;
        }

        if (lines.Count == 0)
            throw new InvalidOperationException("No hay movimientos en ingresos o gastos para cerrar este año.");

        // Net goes to retained earnings
        if (netIncome > 0)
            lines.Add(new JournalLine { AccountId = retainedAccount.Id, Debit = 0, Credit = netIncome });
        else if (netIncome < 0)
            lines.Add(new JournalLine { AccountId = retainedAccount.Id, Debit = Math.Abs(netIncome), Credit = 0 });

        var entry = new JournalEntry
        {
            OrganizationId    = orgId,
            Date              = to,
            Description       = $"Cierre del ejercicio {year}",
            Reference         = $"CIERRE-{year}",
            Status            = JournalStatus.Posted,
            IsYearEndClosing  = true,
            Lines             = lines,
        };

        await _journal.AddAsync(entry, ct);

        var closing = new YearEndClosing
        {
            OrganizationId = orgId,
            Year           = year,
            JournalEntryId = entry.Id,
            ClosedByUserId = userId,
        };
        await _closings.AddAsync(closing, ct);
        await _closings.SaveChangesAsync(ct);

        return new YearEndStatusDto(year, true, null, closing.ClosedAtUtc, entry.Id);
    }

    private static YearEndStatusDto ToDto(int year, YearEndClosing? c) =>
        c is null
            ? new YearEndStatusDto(year, false, null, null, null)
            : new YearEndStatusDto(
                year, true,
                c.ClosedBy is not null ? $"{c.ClosedBy.FirstName} {c.ClosedBy.LastName}" : null,
                c.ClosedAtUtc, c.JournalEntryId);
}
