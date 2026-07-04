using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IReportService
{
    Task<TrialBalanceDto>    GetTrialBalanceAsync(Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IncomeStatementDto> GetIncomeStatementAsync(Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<BalanceSheetDto>    GetBalanceSheetAsync(Guid orgId, DateOnly asOf, CancellationToken ct = default);
}

public class ReportService : IReportService
{
    private const int MaxRangeYears = 5;

    private readonly IReportRepository _reports;
    private readonly IAccountRepository _accounts;

    public ReportService(IReportRepository reports, IAccountRepository accounts)
    {
        _reports = reports;
        _accounts = accounts;
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        ValidateRange(from, to);

        var balances  = await _reports.GetAccountBalancesAsync(orgId, from, to, ct);
        var allAccounts = await _accounts.GetByOrganizationAsync(orgId, ct);
        var accountMap  = allAccounts.ToDictionary(a => a.Id);

        var lines = balances
            .Where(b => accountMap.ContainsKey(b.AccountId))
            .Select(b =>
            {
                var acc = accountMap[b.AccountId];
                var net = b.TotalDebit - b.TotalCredit;
                return new TrialBalanceLineDto(
                    acc.Id, acc.Code, acc.Name, acc.Type,
                    b.TotalDebit, b.TotalCredit,
                    net > 0 ? net : 0,
                    net < 0 ? -net : 0);
            })
            .OrderBy(l => l.Code)
            .ToList();

        var totalDebit        = lines.Sum(l => l.TotalDebit);
        var totalCredit       = lines.Sum(l => l.TotalCredit);
        var totalDebitBalance = lines.Sum(l => l.DebitBalance);
        var totalCreditBalance= lines.Sum(l => l.CreditBalance);

        return new TrialBalanceDto(
            from, to, lines,
            totalDebit, totalCredit,
            totalDebitBalance, totalCreditBalance,
            IsBalanced: Math.Abs(totalDebitBalance - totalCreditBalance) < 0.01m);
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        ValidateRange(from, to);

        var balances    = await _reports.GetAccountBalancesAsync(orgId, from, to, ct);
        var allAccounts = await _accounts.GetByOrganizationAsync(orgId, ct);
        var accountMap  = allAccounts.ToDictionary(a => a.Id);

        IncomeStatementLineDto BuildLine(AccountBalanceData b, decimal amount)
        {
            var acc    = accountMap[b.AccountId];
            Account? parent = acc.ParentId.HasValue && accountMap.TryGetValue(acc.ParentId.Value, out var p) ? p : null;
            return new IncomeStatementLineDto(acc.Id, acc.Code, acc.Name, parent?.Code, parent?.Name, amount);
        }

        var incomeLines = balances
            .Where(b => accountMap.ContainsKey(b.AccountId) && accountMap[b.AccountId].Type == AccountType.Income)
            .Select(b => BuildLine(b, b.TotalCredit - b.TotalDebit))
            .OrderBy(l => l.Code)
            .ToList();

        var expenseLines = balances
            .Where(b => accountMap.ContainsKey(b.AccountId) && accountMap[b.AccountId].Type == AccountType.Expense)
            .Select(b => BuildLine(b, b.TotalDebit - b.TotalCredit))
            .OrderBy(l => l.Code)
            .ToList();

        var totalIncome   = incomeLines.Sum(l => l.Amount);
        var totalExpenses = expenseLines.Sum(l => l.Amount);
        var netIncome     = totalIncome - totalExpenses;

        return new IncomeStatementDto(
            from, to,
            new IncomeStatementSectionDto("Ingresos",  incomeLines,  totalIncome),
            new IncomeStatementSectionDto("Gastos",    expenseLines, totalExpenses),
            netIncome,
            IsProfit: netIncome >= 0);
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(
        Guid orgId, DateOnly asOf, CancellationToken ct = default)
    {
        if (asOf > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("La fecha no puede ser futura.");

        var balances    = await _reports.GetCumulativeBalancesAsync(orgId, asOf, ct);
        var allAccounts = await _accounts.GetByOrganizationAsync(orgId, ct);
        var accountMap  = allAccounts.ToDictionary(a => a.Id);

        // Assets: debit normal balance (debit – credit)
        var assetGroup = BuildGroup("Activos", balances, accountMap, AccountType.Asset,
            b => b.TotalDebit - b.TotalCredit);

        // Liabilities: credit normal balance (credit – debit)
        var liabilityGroup = BuildGroup("Pasivos", balances, accountMap, AccountType.Liability,
            b => b.TotalCredit - b.TotalDebit);

        // Equity accounts: credit normal balance (credit – debit)
        var equityGroup = BuildGroup("Capital", balances, accountMap, AccountType.Equity,
            b => b.TotalCredit - b.TotalDebit);

        // Net income: cumulative income – expenses (goes into equity)
        var totalIncome   = balances
            .Where(b => accountMap.ContainsKey(b.AccountId) && accountMap[b.AccountId].Type == AccountType.Income)
            .Sum(b => b.TotalCredit - b.TotalDebit);
        var totalExpenses = balances
            .Where(b => accountMap.ContainsKey(b.AccountId) && accountMap[b.AccountId].Type == AccountType.Expense)
            .Sum(b => b.TotalDebit - b.TotalCredit);
        var netIncome = totalIncome - totalExpenses;

        var totalEquity              = equityGroup.Total + netIncome;
        var totalLiabilitiesAndEquity = liabilityGroup.Total + totalEquity;

        return new BalanceSheetDto(
            asOf,
            assetGroup,
            liabilityGroup,
            equityGroup,
            netIncome,
            totalEquity,
            totalLiabilitiesAndEquity,
            IsBalanced: Math.Abs(assetGroup.Total - totalLiabilitiesAndEquity) < 0.01m);
    }

    private static BalanceSheetGroupDto BuildGroup(
        string title,
        IEnumerable<AccountBalanceData> balances,
        Dictionary<Guid, Account> accountMap,
        AccountType type,
        Func<AccountBalanceData, decimal> getBalance)
    {
        var sections = balances
            .Where(b => accountMap.ContainsKey(b.AccountId) && accountMap[b.AccountId].Type == type)
            .Select(b => (acc: accountMap[b.AccountId], balance: getBalance(b)))
            .GroupBy(x => x.acc.ParentId)
            .Select(g =>
            {
                Account? parent = g.Key.HasValue && accountMap.TryGetValue(g.Key.Value, out var p) ? p : null;
                var lines = g.OrderBy(x => x.acc.Code)
                    .Select(x => new BalanceSheetLineDto(x.acc.Id, x.acc.Code, x.acc.Name, x.balance))
                    .ToList();
                return new BalanceSheetSectionDto(
                    parent?.Code ?? "",
                    parent?.Name ?? title,
                    lines,
                    lines.Sum(l => l.Balance));
            })
            .OrderBy(s => s.SectionCode)
            .ToList();

        return new BalanceSheetGroupDto(title, sections, sections.Sum(s => s.Subtotal));
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ArgumentException("La fecha de inicio no puede ser mayor que la fecha de fin.");
        if (to.Year - from.Year > MaxRangeYears)
            throw new ArgumentException($"El rango máximo permitido es {MaxRangeYears} años.");
    }
}
