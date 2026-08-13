using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class DashboardServiceTests
{
    private readonly IReportRepository   _reports  = Substitute.For<IReportRepository>();
    private readonly IJournalRepository  _journal  = Substitute.For<IJournalRepository>();
    private readonly IAccountRepository  _accounts = Substitute.For<IAccountRepository>();
    private readonly IOrgSettingsService _settings = Substitute.For<IOrgSettingsService>();
    private readonly IInvoiceRepository  _invoices = Substitute.For<IInvoiceRepository>();
    private readonly DashboardService    _sut;

    private static readonly Guid OrgId   = Guid.NewGuid();
    private static readonly Guid AssetId = Guid.NewGuid();
    private static readonly Guid LiabId  = Guid.NewGuid();
    private static readonly Guid EqId    = Guid.NewGuid();
    private static readonly Guid IncId   = Guid.NewGuid();
    private static readonly Guid ExpId   = Guid.NewGuid();

    private static readonly List<Account> AllAccounts = new()
    {
        MakeAccount(AssetId, AccountType.Asset),
        MakeAccount(LiabId,  AccountType.Liability),
        MakeAccount(EqId,    AccountType.Equity),
        MakeAccount(IncId,   AccountType.Income),
        MakeAccount(ExpId,   AccountType.Expense),
    };

    public DashboardServiceTests()
    {
        _sut = new DashboardService(_reports, _journal, _accounts, _settings, _invoices);

        // Safe defaults — all empty
        _reports.GetCumulativeBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>());
        _reports.GetAccountBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>());
        _reports.GetMonthlyTrendsAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyLineData>());
        _journal.GetRecentAsync(OrgId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<JournalEntry>());
        _accounts.GetByOrganizationAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(AllAccounts);
        _settings.GetAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new OrgSettingsDto(OrgId, "Empresa", null, null, null, null, null, "$", ReportTheme.Minimal));
        _invoices.GetActiveForDashboardAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<ActiveInvoiceSummary>());
    }

    // ── Profit / Loss ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_IsProfit_WhenIncomeExceedsExpenses()
    {
        _reports.GetAccountBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(IncId, TotalDebit: 0,   TotalCredit: 1000), // income = 1000
                new(ExpId, TotalDebit: 600, TotalCredit: 0),    // expenses = 600
            });

        var result = await _sut.GetSummaryAsync(OrgId);

        result.IsProfit.Should().BeTrue();
        result.NetIncome.Should().Be(400);
    }

    [Fact]
    public async Task GetSummaryAsync_IsNotProfit_WhenExpensesExceedIncome()
    {
        _reports.GetAccountBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(IncId, TotalDebit: 0,   TotalCredit: 300), // income = 300
                new(ExpId, TotalDebit: 800, TotalCredit: 0),   // expenses = 800
            });

        var result = await _sut.GetSummaryAsync(OrgId);

        result.IsProfit.Should().BeFalse();
        result.NetIncome.Should().Be(-500);
    }

    // ── Balance Sheet equation ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_IsBalanced_WhenAssetsEqualLiabilitiesPlusEquity()
    {
        // Assets = 1000, Liabilities = 600, Equity accounts = 300, NetIncome = 100
        // totalEquity = 300 + 100 = 400  →  1000 == 600 + 400 ✓
        _reports.GetCumulativeBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(AssetId, TotalDebit: 1000, TotalCredit: 0),
                new(LiabId,  TotalDebit: 0,    TotalCredit: 600),
                new(EqId,    TotalDebit: 0,    TotalCredit: 300),
            });
        _reports.GetAccountBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(IncId, TotalDebit: 0,   TotalCredit: 100),
                new(ExpId, TotalDebit: 0,   TotalCredit: 0),
            });

        var result = await _sut.GetSummaryAsync(OrgId);

        result.TotalAssets.Should().Be(1000);
        result.TotalLiabilities.Should().Be(600);
        result.TotalEquity.Should().Be(400);
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task GetSummaryAsync_IsNotBalanced_WhenEquationDoesNotHold()
    {
        // Assets = 500, Liabilities = 600, Equity = 0, NetIncome = 0  →  500 ≠ 600
        _reports.GetCumulativeBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(AssetId, TotalDebit: 500, TotalCredit: 0),
                new(LiabId,  TotalDebit: 0,   TotalCredit: 600),
            });

        var result = await _sut.GetSummaryAsync(OrgId);

        result.IsBalanced.Should().BeFalse();
    }

    // ── Overdue invoices ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_OverdueCount_ReflectsInvoicesPastDueDate()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var tomorrow  = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        _invoices.GetActiveForDashboardAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<ActiveInvoiceSummary>
            {
                new(Guid.NewGuid(), "F-001", "Cliente A", InvoiceType.Receivable, yesterday, Balance: 200),
                new(Guid.NewGuid(), "F-002", "Cliente B", InvoiceType.Receivable, yesterday, Balance: 300),
                new(Guid.NewGuid(), "F-003", "Proveedor X", InvoiceType.Payable,  tomorrow,  Balance: 150),
            });

        var result = await _sut.GetSummaryAsync(OrgId);

        result.OverdueCount.Should().Be(2);
        result.OverdueAmount.Should().Be(500);
    }

    // ── Pending receivable / payable ───────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_PendingAmounts_SumActiveInvoicesByType()
    {
        var future = DateOnly.FromDateTime(DateTime.Today).AddDays(30);

        _invoices.GetActiveForDashboardAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<ActiveInvoiceSummary>
            {
                new(Guid.NewGuid(), "F-001", "Cliente A", InvoiceType.Receivable, future, Balance: 500),
                new(Guid.NewGuid(), "F-002", "Cliente B", InvoiceType.Receivable, future, Balance: 250),
                new(Guid.NewGuid(), "B-001", "Proveedor", InvoiceType.Payable,    future, Balance: 300),
            });

        var result = await _sut.GetSummaryAsync(OrgId);

        result.PendingReceivable.Should().Be(750);
        result.PendingPayable.Should().Be(300);
    }

    // ── Empty org ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_EmptyOrg_ReturnsZeroValues()
    {
        _accounts.GetByOrganizationAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<Account>());

        var result = await _sut.GetSummaryAsync(OrgId);

        result.TotalAssets.Should().Be(0);
        result.TotalLiabilities.Should().Be(0);
        result.TotalEquity.Should().Be(0);
        result.NetIncome.Should().Be(0);
        result.IsProfit.Should().BeTrue();   // 0 >= 0
        result.IsBalanced.Should().BeTrue(); // |0 - (0+0)| < 0.01
        result.OverdueCount.Should().Be(0);
        result.RecentEntries.Should().BeEmpty();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Account MakeAccount(Guid id, AccountType type) => new()
    {
        Id               = id,
        OrganizationId   = OrgId,
        Code             = "0",
        Name             = type.ToString(),
        Type             = type,
        CashFlowSection  = CashFlowSection.None,
        IsPostable       = true,
    };
}
