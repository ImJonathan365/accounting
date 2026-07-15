using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class YearEndClosingServiceTests
{
    private readonly IYearEndClosingRepository   _closings = Substitute.For<IYearEndClosingRepository>();
    private readonly IAccountRepository          _accounts = Substitute.For<IAccountRepository>();
    private readonly IReportRepository           _reports  = Substitute.For<IReportRepository>();
    private readonly IJournalRepository          _journal  = Substitute.For<IJournalRepository>();
    private readonly IAccountingPeriodRepository _periods  = Substitute.For<IAccountingPeriodRepository>();
    private readonly YearEndClosingService       _sut;

    private static readonly Guid OrgId      = Guid.NewGuid();
    private static readonly Guid UserId     = Guid.NewGuid();
    private static readonly Guid RetainedId = Guid.NewGuid();
    private static readonly Guid IncomeId   = Guid.NewGuid();
    private static readonly Guid ExpenseId  = Guid.NewGuid();
    private const int ClosingYear = 2024;

    public YearEndClosingServiceTests()
    {
        _sut = new YearEndClosingService(_closings, _accounts, _reports, _journal, _periods);

        // Default: year not yet closed
        _closings.GetAsync(OrgId, ClosingYear, Arg.Any<CancellationToken>())
            .Returns((YearEndClosing?)null);

        // Default: all 12 periods closed
        _periods.IsClosedAsync(OrgId, ClosingYear, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Default: retained earnings account exists as Equity
        _accounts.GetByIdAsync(RetainedId, OrgId, Arg.Any<CancellationToken>())
            .Returns(new Account { Id = RetainedId, Type = AccountType.Equity, IsPostable = true, Code = "3.1", Name = "Resultados" });

        // Default: org has income and expense accounts
        _accounts.GetByOrganizationAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<Account>
            {
                new() { Id = IncomeId,  Type = AccountType.Income,  IsPostable = true, Code = "4.1", Name = "Ingresos" },
                new() { Id = ExpenseId, Type = AccountType.Expense, IsPostable = true, Code = "5.1", Name = "Gastos" },
                new() { Id = RetainedId, Type = AccountType.Equity, IsPostable = true, Code = "3.1", Name = "Resultados" },
            });

        // Default: balances for the year
        _reports.GetAccountBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(IncomeId,  0m,    5000m),  // income: credit 5000
                new(ExpenseId, 3000m, 0m),     // expense: debit 3000
            });
    }

    [Fact]
    public async Task CloseYearAsync_FutureYear_ThrowsArgumentException()
    {
        var futureYear = DateTime.UtcNow.Year + 1;

        await _sut.Invoking(s => s.CloseYearAsync(OrgId, UserId, new(futureYear, RetainedId)))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*período ya transcurrido*");
    }

    [Fact]
    public async Task CloseYearAsync_AlreadyClosed_ThrowsInvalidOperation()
    {
        _closings.GetAsync(OrgId, ClosingYear, Arg.Any<CancellationToken>())
            .Returns(new YearEndClosing { Year = ClosingYear });

        await _sut.Invoking(s => s.CloseYearAsync(OrgId, UserId, new(ClosingYear, RetainedId)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya fue cerrado*");
    }

    [Fact]
    public async Task CloseYearAsync_OpenPeriod_ThrowsInvalidOperation()
    {
        _periods.IsClosedAsync(OrgId, ClosingYear, 6, Arg.Any<CancellationToken>())
            .Returns(false); // June is open

        await _sut.Invoking(s => s.CloseYearAsync(OrgId, UserId, new(ClosingYear, RetainedId)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*períodos de 2024*");
    }

    [Fact]
    public async Task CloseYearAsync_RetainedEarningsNotEquity_ThrowsArgumentException()
    {
        _accounts.GetByIdAsync(RetainedId, OrgId, Arg.Any<CancellationToken>())
            .Returns(new Account { Id = RetainedId, Type = AccountType.Asset, IsPostable = true, Code = "1.9", Name = "Otros" });

        await _sut.Invoking(s => s.CloseYearAsync(OrgId, UserId, new(ClosingYear, RetainedId)))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tipo Capital*");
    }

    [Fact]
    public async Task CloseYearAsync_NoIncomeOrExpenseMovements_ThrowsInvalidOperation()
    {
        _reports.GetAccountBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>());

        await _sut.Invoking(s => s.CloseYearAsync(OrgId, UserId, new(ClosingYear, RetainedId)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No hay movimientos*");
    }

    [Fact]
    public async Task CloseYearAsync_Valid_CreatesClosingEntryAndRecord()
    {
        JournalEntry? capturedEntry    = null;
        YearEndClosing? capturedClosing = null;
        await _journal.AddAsync(Arg.Do<JournalEntry>(e => capturedEntry = e), Arg.Any<CancellationToken>());
        await _closings.AddAsync(Arg.Do<YearEndClosing>(c => capturedClosing = c), Arg.Any<CancellationToken>());

        var result = await _sut.CloseYearAsync(OrgId, UserId, new(ClosingYear, RetainedId));

        // Income debit line to zero it out (income = 5000 credit → debit 5000)
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Status.Should().Be(JournalStatus.Posted);
        capturedEntry.Reference.Should().Be($"CIERRE-{ClosingYear}");
        capturedEntry.IsYearEndClosing.Should().BeTrue();

        var incomeLine  = capturedEntry.Lines.First(l => l.AccountId == IncomeId);
        var expenseLine = capturedEntry.Lines.First(l => l.AccountId == ExpenseId);
        var retainedLine = capturedEntry.Lines.First(l => l.AccountId == RetainedId);

        incomeLine.Debit.Should().Be(5000m);    // close income: debit
        incomeLine.Credit.Should().Be(0m);
        expenseLine.Credit.Should().Be(3000m);  // close expense: credit
        expenseLine.Debit.Should().Be(0m);
        retainedLine.Credit.Should().Be(2000m); // net income 5000-3000=2000 → credit retained earnings

        capturedClosing.Should().NotBeNull();
        capturedClosing!.Year.Should().Be(ClosingYear);
        capturedClosing.ClosedByUserId.Should().Be(UserId);

        result.IsClosed.Should().BeTrue();
        result.Year.Should().Be(ClosingYear);
    }

    [Fact]
    public async Task CloseYearAsync_NetLoss_DebitsRetainedEarnings()
    {
        _reports.GetAccountBalancesAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceData>
            {
                new(IncomeId,  0m,    1000m),  // income 1000
                new(ExpenseId, 4000m, 0m),     // expense 4000 → net loss 3000
            });

        JournalEntry? capturedEntry = null;
        await _journal.AddAsync(Arg.Do<JournalEntry>(e => capturedEntry = e), Arg.Any<CancellationToken>());
        await _closings.AddAsync(Arg.Any<YearEndClosing>(), Arg.Any<CancellationToken>());

        await _sut.CloseYearAsync(OrgId, UserId, new(ClosingYear, RetainedId));

        var retainedLine = capturedEntry!.Lines.First(l => l.AccountId == RetainedId);
        retainedLine.Debit.Should().Be(3000m); // net loss → debit retained earnings
        retainedLine.Credit.Should().Be(0m);
    }

    [Fact]
    public async Task GetStatusAsync_NotClosed_ReturnsFalseStatus()
    {
        _closings.GetAsync(OrgId, ClosingYear, Arg.Any<CancellationToken>())
            .Returns((YearEndClosing?)null);

        var result = await _sut.GetStatusAsync(OrgId, ClosingYear);

        result.IsClosed.Should().BeFalse();
        result.Year.Should().Be(ClosingYear);
    }
}
