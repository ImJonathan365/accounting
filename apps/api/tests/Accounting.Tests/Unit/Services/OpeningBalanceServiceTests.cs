using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class OpeningBalanceServiceTests
{
    private readonly IJournalRepository          _journal  = Substitute.For<IJournalRepository>();
    private readonly IAccountRepository          _accounts = Substitute.For<IAccountRepository>();
    private readonly IAccountingPeriodRepository _periods  = Substitute.For<IAccountingPeriodRepository>();
    private readonly OpeningBalanceService       _sut;

    private static readonly Guid OrgId    = Guid.NewGuid();
    private static readonly Guid AccIdA   = Guid.NewGuid();
    private static readonly Guid AccIdB   = Guid.NewGuid();

    public OpeningBalanceServiceTests()
    {
        _sut = new OpeningBalanceService(_journal, _accounts, _periods);

        _periods.IsClosedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _journal.ExistsWithReferenceAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _accounts.GetByOrganizationAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(AccIdA), MakeAccount(AccIdB) });
    }

    [Fact]
    public async Task SetAsync_InvalidDate_ThrowsArgumentException()
    {
        var dto = new SetOpeningBalancesRequest("not-a-date", null, Lines(AccIdA, 100m, AccIdB, 100m));

        await _sut.Invoking(s => s.SetAsync(OrgId, dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Fecha inválida*");
    }

    [Fact]
    public async Task SetAsync_AllZeroLines_ThrowsInvalidOperation()
    {
        var dto = new SetOpeningBalancesRequest("2025-01-01", null, Lines(AccIdA, 0m, AccIdB, 0m));

        await _sut.Invoking(s => s.SetAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*al menos una línea*");
    }

    [Fact]
    public async Task SetAsync_UnbalancedLines_ThrowsInvalidOperation()
    {
        var dto = new SetOpeningBalancesRequest("2025-01-01", null, Lines(AccIdA, 100m, AccIdB, 90m));

        await _sut.Invoking(s => s.SetAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no balancea*");
    }

    [Fact]
    public async Task SetAsync_ClosedPeriod_ThrowsInvalidOperation()
    {
        _periods.IsClosedAsync(OrgId, 2025, 1, Arg.Any<CancellationToken>()).Returns(true);

        var dto = new SetOpeningBalancesRequest("2025-01-01", null, Lines(AccIdA, 100m, AccIdB, 100m));

        await _sut.Invoking(s => s.SetAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*período*cerrado*");
    }

    [Fact]
    public async Task SetAsync_ExistingOpeningBalances_ThrowsInvalidOperation()
    {
        _journal.ExistsWithReferenceAsync(OrgId, "SALDOS-INICIALES", Arg.Any<CancellationToken>())
            .Returns(true);

        var dto = new SetOpeningBalancesRequest("2025-01-01", null, Lines(AccIdA, 100m, AccIdB, 100m));

        await _sut.Invoking(s => s.SetAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya existen saldos iniciales*");
    }

    [Fact]
    public async Task SetAsync_AccountNotInOrg_ThrowsInvalidOperation()
    {
        var foreignId = Guid.NewGuid();
        _accounts.GetByOrganizationAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(AccIdA) }); // AccIdB not included

        var dto = new SetOpeningBalancesRequest("2025-01-01", null, Lines(AccIdA, 100m, foreignId, 100m));

        await _sut.Invoking(s => s.SetAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no pertenecen a la organización*");
    }

    [Fact]
    public async Task SetAsync_ValidBalancedEntry_CreatesJournalEntry()
    {
        JournalEntry? captured = null;
        await _journal.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        var dto = new SetOpeningBalancesRequest("2025-01-01", "Apertura", Lines(AccIdA, 500m, AccIdB, 500m));

        var result = await _sut.SetAsync(OrgId, dto);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(JournalStatus.Posted);
        captured.Reference.Should().Be("SALDOS-INICIALES");
        captured.Lines.Should().HaveCount(2);
        result.TotalDebit.Should().Be(500m);
        result.TotalCredit.Should().Be(500m);
    }

    private static Account MakeAccount(Guid id) =>
        new() { Id = id, OrganizationId = OrgId, Code = "1.1", Name = "Cuenta", Type = AccountType.Asset, IsPostable = true };

    private static List<OpeningBalanceLineRequest> Lines(Guid idA, decimal amtA, Guid idB, decimal amtB) =>
        new() { new(idA, amtA, 0m), new(idB, 0m, amtB) };
}
