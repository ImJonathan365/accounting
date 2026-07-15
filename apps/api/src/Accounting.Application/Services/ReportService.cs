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
    Task<LedgerDto>     GetLedgerAsync(Guid orgId, Guid accountId, DateOnly from, DateOnly to, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<CashFlowDto>   GetCashFlowAsync(Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default);
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

    public async Task<LedgerDto> GetLedgerAsync(
        Guid orgId, Guid accountId, DateOnly from, DateOnly to,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        ValidateRange(from, to);
        if (page    < 1) page     = 1;
        if (pageSize < 1) pageSize = 50;

        var account = await _accounts.GetByIdAsync(accountId, orgId, ct)
            ?? throw new KeyNotFoundException($"Cuenta {accountId} no encontrada.");

        // Credit-normal accounts (Liability, Income, Equity) show positive balance when Credit > Debit
        var sign = account.Type is AccountType.Liability or AccountType.Income or AccountType.Equity
            ? -1m : 1m;

        var openingBalance = sign * await _reports.GetAccountOpeningBalanceAsync(orgId, accountId, from, ct);

        var skip = (page - 1) * pageSize;
        var (rawLines, total) = await _reports.GetLedgerLinesPagedAsync(orgId, accountId, from, to, page, pageSize, ct);

        // Balance accumulated by lines BEFORE this page (within the date range)
        var balanceBeforePage = skip > 0
            ? sign * await _reports.GetLedgerBalanceInRangeAsync(orgId, accountId, from, to, skip, ct)
            : 0m;

        var pageOpeningBalance = openingBalance + balanceBeforePage;
        var running = pageOpeningBalance;
        var lines = rawLines.Select(l =>
        {
            running += sign * (l.Debit - l.Credit);
            return new LedgerLineDto(l.EntryId, l.Date, l.Description, l.Reference, l.Debit, l.Credit, running);
        }).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new LedgerDto(
            accountId, account.Code, account.Name, account.Type,
            from, to, openingBalance, lines, running,
            total, page, pageSize, totalPages);
    }

    public async Task<CashFlowDto> GetCashFlowAsync(
        Guid orgId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        ValidateRange(from, to);

        var allAccounts  = await _accounts.GetByOrganizationAsync(orgId, ct);
        var beginOf      = from.AddDays(-1);
        var beginBalances = await _reports.GetCumulativeBalancesAsync(orgId, beginOf, ct);
        var endBalances   = await _reports.GetCumulativeBalancesAsync(orgId, to, ct);
        var periodBalances = await _reports.GetAccountBalancesAsync(orgId, from, to, ct);

        var beginMap = beginBalances.ToDictionary(b => b.AccountId);
        var endMap   = endBalances.ToDictionary(b => b.AccountId);
        var periodMap = periodBalances.ToDictionary(b => b.AccountId);

        // Compute current balance for an account from its cumulative data
        static decimal GetBalance(Guid id, Dictionary<Guid, AccountBalanceData> map, AccountType type)
        {
            if (!map.TryGetValue(id, out var b)) return 0m;
            return type == AccountType.Asset || type == AccountType.Expense
                ? b.TotalDebit - b.TotalCredit
                : b.TotalCredit - b.TotalDebit;
        }

        // Cash: beginning and ending balance
        var cashAccounts = allAccounts.Where(a => a.CashFlowSection == CashFlowSection.Cash).ToList();
        var beginCash = cashAccounts.Sum(a => GetBalance(a.Id, beginMap, a.Type));
        var endCash   = cashAccounts.Sum(a => GetBalance(a.Id, endMap,   a.Type));

        // Net Income (income - expense for the period)
        decimal periodIncome   = allAccounts
            .Where(a => a.Type == AccountType.Income)
            .Sum(a => periodMap.TryGetValue(a.Id, out var b) ? b.TotalCredit - b.TotalDebit : 0m);
        decimal periodExpenses = allAccounts
            .Where(a => a.Type == AccountType.Expense)
            .Sum(a => periodMap.TryGetValue(a.Id, out var b) ? b.TotalDebit - b.TotalCredit : 0m);
        decimal netIncome = periodIncome - periodExpenses;

        // Build a cash flow section from accounts with a given CashFlowSection tag
        CashFlowSectionDto BuildSection(string title, CashFlowSection section, decimal sectionNetIncome = 0m)
        {
            var accounts = allAccounts.Where(a => a.CashFlowSection == section).ToList();
            var lines = new List<CashFlowLineDto>();

            foreach (var acc in accounts)
            {
                var beginBal = GetBalance(acc.Id, beginMap, acc.Type);
                var endBal   = GetBalance(acc.Id, endMap,   acc.Type);
                var change   = endBal - beginBal;

                // For assets: increase = cash outflow (negative); for liabilities/equity: increase = cash inflow (positive)
                var cashEffect = acc.Type == AccountType.Asset ? -change : change;

                if (cashEffect != 0m)
                    lines.Add(new CashFlowLineDto(acc.Code, acc.Name, cashEffect));
            }

            var adjustments = lines.Sum(l => l.Amount);
            return new CashFlowSectionDto(title, lines, sectionNetIncome, sectionNetIncome + adjustments);
        }

        var operating  = BuildSection("Actividades de Operación",    CashFlowSection.Operating, netIncome);
        var investing  = BuildSection("Actividades de Inversión",    CashFlowSection.Investing);
        var financing  = BuildSection("Actividades de Financiamiento", CashFlowSection.Financing);
        var netChange  = operating.Total + investing.Total + financing.Total;

        return new CashFlowDto(from, to, beginCash, operating, investing, financing, netChange, endCash);
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ArgumentException("La fecha de inicio no puede ser mayor que la fecha de fin.");
        if (to.Year - from.Year > MaxRangeYears)
            throw new ArgumentException($"El rango máximo permitido es {MaxRangeYears} años.");
    }
}
