using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class ReportServiceTests
{
    private readonly IReportRepository  _reportRepo  = Substitute.For<IReportRepository>();
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly ReportService _sut;

    private static readonly Guid BankId    = Guid.NewGuid();
    private static readonly Guid IncId     = Guid.NewGuid();
    private static readonly Guid ExpId     = Guid.NewGuid();
    private static readonly Guid CapId     = Guid.NewGuid();
    private static readonly Guid LiabId   = Guid.NewGuid();

    private static readonly Account BankAccount = MakeAccount(BankId, "1.1.02", "Banco",               AccountType.Asset);
    private static readonly Account IncAccount  = MakeAccount(IncId,  "4.1",    "Ingresos por ventas", AccountType.Income);
    private static readonly Account ExpAccount  = MakeAccount(ExpId,  "5.1.01", "Sueldos",             AccountType.Expense);
    private static readonly Account CapAccount  = MakeAccount(CapId,  "3.1",    "Capital social",      AccountType.Equity);
    private static readonly Account LiabAccount = MakeAccount(LiabId, "2.1.01", "Cuentas por pagar",   AccountType.Liability);

    private static readonly List<Account> AllAccounts =
        new() { BankAccount, IncAccount, ExpAccount, CapAccount, LiabAccount };

    public ReportServiceTests()
    {
        _sut = new ReportService(_reportRepo, _accountRepo);
        _accountRepo.GetByOrganizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(AllAccounts);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_FromGreaterThanTo_ThrowsArgumentException()
    {
        var from = new DateOnly(2026, 6, 1);
        var to   = new DateOnly(2026, 1, 1);

        await _sut.Invoking(s => s.GetTrialBalanceAsync(Guid.NewGuid(), from, to))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*fecha de inicio*");
    }

    [Fact]
    public async Task GetTrialBalanceAsync_RangeExceedsFiveYears_ThrowsArgumentException()
    {
        var from = new DateOnly(2020, 1, 1);
        var to   = new DateOnly(2026, 1, 1);

        await _sut.Invoking(s => s.GetTrialBalanceAsync(Guid.NewGuid(), from, to))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*rango máximo*");
    }

    [Fact]
    public async Task GetBalanceSheetAsync_FutureDate_ThrowsArgumentException()
    {
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        await _sut.Invoking(s => s.GetBalanceSheetAsync(Guid.NewGuid(), future))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*futura*");
    }

    [Fact]
    public async Task GetTrialBalanceAsync_BalancedMovements_IsBalancedTrue()
    {
        // Bank debited 1000, Income credited 1000 → perfect double-entry
        SetupRangeBalances(
            (BankId, debit: 1000m, credit: 0m),
            (IncId,  debit: 0m,    credit: 1000m));

        var result = await _sut.GetTrialBalanceAsync(Guid.NewGuid(), Jan1(), Today());

        result.IsBalanced.Should().BeTrue();
        result.TotalDebit.Should().Be(1000m);
        result.TotalCredit.Should().Be(1000m);
        result.TotalDebitBalance.Should().Be(1000m);
        result.TotalCreditBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_LineDebitAndCreditBalance_ComputedCorrectly()
    {
        SetupRangeBalances((BankId, debit: 1500m, credit: 300m));

        var result = await _sut.GetTrialBalanceAsync(Guid.NewGuid(), Jan1(), Today());

        var bankLine = result.Lines.Single(l => l.AccountId == BankId);
        bankLine.DebitBalance.Should().Be(1200m);
        bankLine.CreditBalance.Should().Be(0m);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_NoMovements_ReturnsEmptyLines()
    {
        _reportRepo.GetAccountBalancesAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>());

        var result = await _sut.GetTrialBalanceAsync(Guid.NewGuid(), Jan1(), Today());

        result.Lines.Should().BeEmpty();
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task GetTrialBalanceAsync_LinesOrderedByCode()
    {
        SetupRangeBalances(
            (IncId,  debit: 0m,   credit: 500m),
            (BankId, debit: 500m, credit: 0m));

        var result = await _sut.GetTrialBalanceAsync(Guid.NewGuid(), Jan1(), Today());

        result.Lines.Select(l => l.Code).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetIncomeStatementAsync_Profitable_NetIncomePositive()
    {
        SetupRangeBalances(
            (IncId, debit: 0m,    credit: 2000m),
            (ExpId, debit: 800m,  credit: 0m));

        var result = await _sut.GetIncomeStatementAsync(Guid.NewGuid(), Jan1(), Today());

        result.Income.Total.Should().Be(2000m);
        result.Expenses.Total.Should().Be(800m);
        result.NetIncome.Should().Be(1200m);
        result.IsProfit.Should().BeTrue();
    }

    [Fact]
    public async Task GetIncomeStatementAsync_Loss_IsProfit_False()
    {
        SetupRangeBalances(
            (IncId, debit: 0m,    credit: 500m),
            (ExpId, debit: 900m,  credit: 0m));

        var result = await _sut.GetIncomeStatementAsync(Guid.NewGuid(), Jan1(), Today());

        result.NetIncome.Should().Be(-400m);
        result.IsProfit.Should().BeFalse();
    }

    [Fact]
    public async Task GetIncomeStatementAsync_NoMovements_ZeroNetIncome()
    {
        _reportRepo.GetAccountBalancesAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>());

        var result = await _sut.GetIncomeStatementAsync(Guid.NewGuid(), Jan1(), Today());

        result.NetIncome.Should().Be(0m);
        result.Income.Lines.Should().BeEmpty();
        result.Expenses.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBalanceSheetAsync_BasicDoubleEntry_IsBalanced()
    {
        // Bank (asset) debited 1000, Income credited 1000
        SetupCumulativeBalances(
            (BankId, debit: 1000m, credit: 0m),
            (IncId,  debit: 0m,    credit: 1000m));

        var result = await _sut.GetBalanceSheetAsync(Guid.NewGuid(), Today());

        result.Assets.Total.Should().Be(1000m);
        result.Liabilities.Total.Should().Be(0m);
        result.Equity.Total.Should().Be(0m);
        result.NetIncome.Should().Be(1000m);
        result.TotalEquity.Should().Be(1000m);
        result.TotalLiabilitiesAndEquity.Should().Be(1000m);
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task GetBalanceSheetAsync_WithLiabilityAndEquity_EquationHolds()
    {
        // Capital aporte 5000 (debit bank, credit capital)
        // Compra a crédito 2000 (debit expense, credit payable)
        SetupCumulativeBalances(
            (BankId,  debit: 5000m, credit: 0m),
            (CapId,   debit: 0m,    credit: 5000m),
            (ExpId,   debit: 2000m, credit: 0m),
            (LiabId,  debit: 0m,    credit: 2000m));

        var result = await _sut.GetBalanceSheetAsync(Guid.NewGuid(), Today());

        result.Assets.Total.Should().Be(5000m);
        result.Liabilities.Total.Should().Be(2000m);
        result.Equity.Total.Should().Be(5000m);
        result.NetIncome.Should().Be(-2000m);
        result.TotalEquity.Should().Be(3000m);
        result.TotalLiabilitiesAndEquity.Should().Be(5000m);
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task GetBalanceSheetAsync_AssetWithCredit_NegativeBalance()
    {
        // Depreciación: debit fixed assets, credit accumulated depreciation (both are assets)
        var fixedId = Guid.NewGuid();
        var deprId  = Guid.NewGuid();
        var fixedAcc = MakeAccount(fixedId, "1.2.01", "Propiedades", AccountType.Asset);
        var deprAcc  = MakeAccount(deprId,  "1.2.02", "Depreciación acumulada", AccountType.Asset);

        var extAccounts = new List<Account>(AllAccounts) { fixedAcc, deprAcc };
        _accountRepo.GetByOrganizationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(extAccounts);

        _reportRepo.GetCumulativeBalancesAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(fixedId, TotalDebit: 10_000m, TotalCredit: 0m),
                new(deprId,  TotalDebit: 0m,      TotalCredit: 2_000m),
            });

        var result = await _sut.GetBalanceSheetAsync(Guid.NewGuid(), Today());

        result.Assets.Total.Should().Be(8_000m);
    }

    private void SetupRangeBalances(params (Guid id, decimal debit, decimal credit)[] data)
    {
        var list = data.Select(d => new AccountBalanceData(d.id, d.debit, d.credit)).ToList();
        _reportRepo.GetAccountBalancesAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(list);
    }

    private void SetupCumulativeBalances(params (Guid id, decimal debit, decimal credit)[] data)
    {
        var list = data.Select(d => new AccountBalanceData(d.id, d.debit, d.credit)).ToList();
        _reportRepo.GetCumulativeBalancesAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(list);
    }

    private static Account MakeAccount(Guid id, string code, string name, AccountType type) =>
        new() { Id = id, Code = code, Name = name, Type = type, IsPostable = true };

    private static DateOnly Jan1() => new(DateTime.Today.Year, 1, 1);
    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Today);
}
