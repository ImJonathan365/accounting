using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid orgId, CancellationToken ct = default);
}

public class DashboardService : IDashboardService
{
    private readonly IReportRepository    _reports;
    private readonly IJournalRepository   _journal;
    private readonly IAccountRepository   _accounts;
    private readonly IOrgSettingsService  _settings;

    public DashboardService(
        IReportRepository   reports,
        IJournalRepository  journal,
        IAccountRepository  accounts,
        IOrgSettingsService settings)
    {
        _reports  = reports;
        _journal  = journal;
        _accounts = accounts;
        _settings = settings;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid orgId, CancellationToken ct = default)
    {
        var today     = DateOnly.FromDateTime(DateTime.Today);
        var yearStart = new DateOnly(today.Year, 1, 1);

        // Sequential: EF DbContext is not thread-safe for concurrent async operations
        var cumBalances    = await _reports.GetCumulativeBalancesAsync(orgId, today, ct);
        var periodBalances = await _reports.GetAccountBalancesAsync(orgId, yearStart, today, ct);
        var recentEntries  = await _journal.GetRecentAsync(orgId, 5, ct);
        var orgSettings    = await _settings.GetAsync(orgId, ct);
        var allAccounts    = await _accounts.GetByOrganizationAsync(orgId, ct);

        var accountMap = allAccounts.ToDictionary(a => a.Id);

        decimal totalAssets = cumBalances
            .Where(b => accountMap.TryGetValue(b.AccountId, out var a) && a.Type == AccountType.Asset)
            .Sum(b => b.TotalDebit - b.TotalCredit);

        decimal totalLiabilities = cumBalances
            .Where(b => accountMap.TryGetValue(b.AccountId, out var a) && a.Type == AccountType.Liability)
            .Sum(b => b.TotalCredit - b.TotalDebit);

        decimal totalEquityAccounts = cumBalances
            .Where(b => accountMap.TryGetValue(b.AccountId, out var a) && a.Type == AccountType.Equity)
            .Sum(b => b.TotalCredit - b.TotalDebit);

        decimal periodIncome = periodBalances
            .Where(b => accountMap.TryGetValue(b.AccountId, out var a) && a.Type == AccountType.Income)
            .Sum(b => b.TotalCredit - b.TotalDebit);

        decimal periodExpenses = periodBalances
            .Where(b => accountMap.TryGetValue(b.AccountId, out var a) && a.Type == AccountType.Expense)
            .Sum(b => b.TotalDebit - b.TotalCredit);

        decimal netIncome    = periodIncome - periodExpenses;
        decimal totalEquity  = totalEquityAccounts + netIncome;
        bool    isBalanced   = Math.Abs(totalAssets - (totalLiabilities + totalEquity)) < 0.01m;

        var recent = recentEntries.Select(e => new RecentEntryDto(
            e.Id, e.Date, e.Description, e.Reference,
            e.Lines.Sum(l => l.Debit), e.Status)).ToList();

        var monthName = today.ToString("MMMM", new System.Globalization.CultureInfo("es-GT"));
        var period    = $"Enero – {char.ToUpper(monthName[0]) + monthName[1..]} {today.Year}";

        return new DashboardSummaryDto(
            totalAssets,
            totalLiabilities,
            totalEquity,
            netIncome,
            IsProfit: netIncome >= 0,
            isBalanced,
            recent,
            orgSettings.CurrencySymbol,
            period);
    }
}
