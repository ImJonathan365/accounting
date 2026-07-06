using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Enums;
using System.Globalization;

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
        var today      = DateOnly.FromDateTime(DateTime.Today);
        var yearStart  = new DateOnly(today.Year, 1, 1);
        var trendStart = today.AddMonths(-5).AddDays(1 - today.AddMonths(-5).Day); // first day 6 months ago

        var cumBalances    = await _reports.GetCumulativeBalancesAsync(orgId, today, ct);
        var periodBalances = await _reports.GetAccountBalancesAsync(orgId, yearStart, today, ct);
        var recentEntries  = await _journal.GetRecentAsync(orgId, 5, ct);
        var orgSettings    = await _settings.GetAsync(orgId, ct);
        var allAccounts    = await _accounts.GetByOrganizationAsync(orgId, ct);
        var monthlyLines   = await _reports.GetMonthlyTrendsAsync(orgId, trendStart, today, ct);

        var accountMap = allAccounts.ToDictionary(a => a.Id);

        // ── Core KPIs ──────────────────────────────────────────────────────────

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

        decimal netIncome   = periodIncome - periodExpenses;
        decimal totalEquity = totalEquityAccounts + netIncome;
        bool    isBalanced  = Math.Abs(totalAssets - (totalLiabilities + totalEquity)) < 0.01m;

        // ── Monthly Trend (last 6 months) ──────────────────────────────────────

        var incomeIds  = allAccounts.Where(a => a.Type == AccountType.Income).Select(a => a.Id).ToHashSet();
        var expenseIds = allAccounts.Where(a => a.Type == AccountType.Expense).Select(a => a.Id).ToHashSet();

        var monthlyTrend = Enumerable.Range(0, 6)
            .Select(i => today.AddMonths(-5 + i))
            .Select(d =>
            {
                var (yr, mo) = (d.Year, d.Month);
                var rows = monthlyLines.Where(l => l.Year == yr && l.Month == mo).ToList();
                var income  = rows.Where(l => incomeIds.Contains(l.AccountId)).Sum(l => l.Credit - l.Debit);
                var expense = rows.Where(l => expenseIds.Contains(l.AccountId)).Sum(l => l.Debit - l.Credit);
                var label   = new DateTime(yr, mo, 1).ToString("MMM yyyy", new CultureInfo("es-GT"));
                return new MonthlyTrendDto(yr, mo, label, income < 0 ? 0 : income, expense < 0 ? 0 : expense);
            })
            .ToList();

        // ── Financial Ratios ───────────────────────────────────────────────────

        // Liquidity: (Cash + Operating Assets) / Operating Liabilities
        var cashAndOpAssets = cumBalances
            .Where(b => accountMap.TryGetValue(b.AccountId, out var a)
                     && a.Type == AccountType.Asset
                     && (a.CashFlowSection == CashFlowSection.Cash || a.CashFlowSection == CashFlowSection.Operating))
            .Sum(b => b.TotalDebit - b.TotalCredit);

        var opLiabilities = cumBalances
            .Where(b => accountMap.TryGetValue(b.AccountId, out var a)
                     && a.Type == AccountType.Liability
                     && a.CashFlowSection == CashFlowSection.Operating)
            .Sum(b => b.TotalCredit - b.TotalDebit);

        decimal? liquidityRatio = opLiabilities > 0 ? Math.Round(cashAndOpAssets / opLiabilities, 2) : null;

        decimal debtRatio = totalAssets > 0
            ? Math.Round(totalLiabilities / totalAssets, 4)
            : 0m;

        decimal? netProfitMargin = periodIncome > 0
            ? Math.Round(netIncome / periodIncome * 100, 2)
            : null;

        var ratios = new FinancialRatiosDto(liquidityRatio, debtRatio, netProfitMargin);

        // ── Build response ─────────────────────────────────────────────────────

        var recent = recentEntries.Select(e => new RecentEntryDto(
            e.Id, e.Date, e.Description, e.Reference,
            e.Lines.Sum(l => l.Debit), e.Status)).ToList();

        var monthName = today.ToString("MMMM", new CultureInfo("es-GT"));
        var period    = $"Enero – {char.ToUpper(monthName[0]) + monthName[1..]} {today.Year}";

        return new DashboardSummaryDto(
            totalAssets, totalLiabilities, totalEquity, netIncome,
            IsProfit: netIncome >= 0, isBalanced, recent,
            orgSettings.CurrencySymbol, period, monthlyTrend, ratios);
    }
}
