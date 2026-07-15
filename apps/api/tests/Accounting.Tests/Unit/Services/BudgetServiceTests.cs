using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class BudgetServiceTests
{
    private readonly IBudgetRepository  _repo     = Substitute.For<IBudgetRepository>();
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IReportRepository  _reports  = Substitute.For<IReportRepository>();
    private readonly BudgetService      _sut;

    private static readonly Guid OrgId    = Guid.NewGuid();
    private static readonly Guid BudgetId = Guid.NewGuid();
    private static readonly Guid AccId    = Guid.NewGuid();

    public BudgetServiceTests()
    {
        _sut = new BudgetService(_repo, _accounts, _reports);

        _repo.GetByIdAsync(OrgId, BudgetId, Arg.Any<CancellationToken>())
            .Returns(MakeBudget());
        _accounts.GetByIdAsync(AccId, OrgId, Arg.Any<CancellationToken>())
            .Returns(MakeAccount(AccId, AccountType.Income));
        _accounts.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(AccId, AccountType.Income) });
        _reports.GetMonthlyTrendsAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyLineData>());
    }

    [Fact]
    public async Task CreateAsync_Valid_AddsBudget()
    {
        Budget? captured = null;
        await _repo.AddAsync(Arg.Do<Budget>(b => captured = b), Arg.Any<CancellationToken>());

        await _sut.CreateAsync(OrgId, new CreateBudgetDto("Presupuesto Anual", 2025));

        captured.Should().NotBeNull();
        captured!.Year.Should().Be(2025);
        captured.Name.Should().Be("Presupuesto Anual");
        captured.OrganizationId.Should().Be(OrgId);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Budget?)null);

        await _sut.Invoking(s => s.GetByIdAsync(OrgId, Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Presupuesto no encontrado*");
    }

    [Fact]
    public async Task UpsertLineAsync_AccountNotFound_ThrowsKeyNotFoundException()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), OrgId, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        await _sut.Invoking(s => s.UpsertLineAsync(OrgId, BudgetId, new UpsertBudgetLineDto(Guid.NewGuid(), 1, 1000m)))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Cuenta no encontrada*");
    }

    [Fact]
    public async Task UpsertLineAsync_NewLine_AddsLine()
    {
        var budget = MakeBudget(); // empty lines
        _repo.GetByIdAsync(OrgId, BudgetId, Arg.Any<CancellationToken>()).Returns(budget);

        // Updated budget returned after save
        var updatedBudget = MakeBudget();
        updatedBudget.Lines.Add(new BudgetLine { BudgetId = BudgetId, AccountId = AccId, Month = 3, Amount = 1500m });
        _repo.GetByIdAsync(OrgId, BudgetId, Arg.Any<CancellationToken>()).Returns(budget, updatedBudget);

        var result = await _sut.UpsertLineAsync(OrgId, BudgetId, new UpsertBudgetLineDto(AccId, 3, 1500m));

        budget.Lines.Should().ContainSingle(l => l.Month == 3 && l.Amount == 1500m);
    }

    [Fact]
    public async Task UpsertLineAsync_ExistingLine_UpdatesAmount()
    {
        var budget = MakeBudget();
        budget.Lines.Add(new BudgetLine { BudgetId = BudgetId, AccountId = AccId, Month = 1, Amount = 800m });
        _repo.GetByIdAsync(OrgId, BudgetId, Arg.Any<CancellationToken>()).Returns(budget);

        await _sut.UpsertLineAsync(OrgId, BudgetId, new UpsertBudgetLineDto(AccId, 1, 1200m));

        budget.Lines.Single(l => l.Month == 1).Amount.Should().Be(1200m);
    }

    [Fact]
    public async Task UpsertLineAsync_ZeroAmount_RemovesLine()
    {
        var budget = MakeBudget();
        var existingLine = new BudgetLine { BudgetId = BudgetId, AccountId = AccId, Month = 2, Amount = 500m };
        budget.Lines.Add(existingLine);
        _repo.GetByIdAsync(OrgId, BudgetId, Arg.Any<CancellationToken>()).Returns(budget);

        await _sut.UpsertLineAsync(OrgId, BudgetId, new UpsertBudgetLineDto(AccId, 2, 0m));

        _repo.Received(1).RemoveLine(existingLine);
    }

    [Fact]
    public async Task GetVsActualAsync_IncomeCreditNormal_ActualIsCredit_minus_Debit()
    {
        var incomeAccId = Guid.NewGuid();
        var budget      = MakeBudget();
        budget.Lines.Add(new BudgetLine { BudgetId = BudgetId, AccountId = incomeAccId, Month = 1, Amount = 1000m, Account = MakeAccount(incomeAccId, AccountType.Income) });

        _repo.GetByIdAsync(OrgId, BudgetId, Arg.Any<CancellationToken>()).Returns(budget);
        _accounts.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(incomeAccId, AccountType.Income) });

        // Actual: credit 1200, debit 200 → income balance = 1200 - 200 = 1000
        _reports.GetMonthlyTrendsAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyLineData> { new(2025, 1, incomeAccId, 200m, 1200m) });

        var result = await _sut.GetVsActualAsync(OrgId, BudgetId);

        var line = result.Lines.Single();
        line.Actual[0].Should().Be(1000m);  // credit - debit
    }

    [Fact]
    public async Task GetVsActualAsync_AssetDebitNormal_ActualIsDebit_minus_Credit()
    {
        var assetAccId = Guid.NewGuid();
        var budget     = MakeBudget();
        budget.Lines.Add(new BudgetLine { BudgetId = BudgetId, AccountId = assetAccId, Month = 1, Amount = 2000m, Account = MakeAccount(assetAccId, AccountType.Asset) });

        _repo.GetByIdAsync(OrgId, BudgetId, Arg.Any<CancellationToken>()).Returns(budget);
        _accounts.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(assetAccId, AccountType.Asset) });

        // Actual: debit 1800, credit 200 → asset balance = 1800 - 200 = 1600
        _reports.GetMonthlyTrendsAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<MonthlyLineData> { new(2025, 1, assetAccId, 1800m, 200m) });

        var result = await _sut.GetVsActualAsync(OrgId, BudgetId);

        var line = result.Lines.Single();
        line.Actual[0].Should().Be(1600m);  // debit - credit
    }

    private static Account MakeAccount(Guid id, AccountType type) =>
        new() { Id = id, Code = "4.1", Name = "Cuenta", Type = type, IsPostable = true };

    private static Budget MakeBudget() =>
        new() { Id = BudgetId, OrganizationId = OrgId, Year = 2025, Name = "Presupuesto Anual", Lines = new List<BudgetLine>() };
}
