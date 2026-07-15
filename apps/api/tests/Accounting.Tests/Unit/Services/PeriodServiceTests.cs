using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class PeriodServiceTests
{
    private readonly IAccountingPeriodRepository _repo = Substitute.For<IAccountingPeriodRepository>();
    private readonly PeriodService               _sut;

    private static readonly Guid OrgId  = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public PeriodServiceTests()
    {
        _sut = new PeriodService(_repo);
        _repo.IsClosedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task CloseAsync_AlreadyClosed_ThrowsInvalidOperation()
    {
        _repo.IsClosedAsync(OrgId, 2025, 3, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.Invoking(s => s.CloseAsync(OrgId, UserId, new ClosePeriodDto(2025, 3)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya está cerrado*");
    }

    [Fact]
    public async Task CloseAsync_FuturePeriod_ThrowsInvalidOperation()
    {
        var today     = DateTime.UtcNow;
        var futureYear  = today.Year + 1;

        await _sut.Invoking(s => s.CloseAsync(OrgId, UserId, new ClosePeriodDto(futureYear, 1)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*período futuro*");
    }

    [Fact]
    public async Task CloseAsync_ValidPastPeriod_AddsPeriodRecord()
    {
        var user = new User { Id = UserId, FirstName = "Ana", LastName = "López" };
        var saved = new AccountingPeriod { Year = 2025, Month = 1, ClosedByUserId = UserId, ClosedBy = user };

        _repo.GetAsync(OrgId, 2025, 1, Arg.Any<CancellationToken>()).Returns(saved);

        AccountingPeriod? captured = null;
        await _repo.AddAsync(Arg.Do<AccountingPeriod>(p => captured = p), Arg.Any<CancellationToken>());

        await _sut.CloseAsync(OrgId, UserId, new ClosePeriodDto(2025, 1));

        captured.Should().NotBeNull();
        captured!.OrganizationId.Should().Be(OrgId);
        captured.Year.Should().Be(2025);
        captured.Month.Should().Be(1);
        captured.ClosedByUserId.Should().Be(UserId);
    }

    [Fact]
    public async Task ReopenAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetAsync(OrgId, 2025, 5, Arg.Any<CancellationToken>())
            .Returns((AccountingPeriod?)null);

        await _sut.Invoking(s => s.ReopenAsync(OrgId, 2025, 5))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ReopenAsync_Found_RemovesPeriod()
    {
        var period = new AccountingPeriod { Year = 2025, Month = 2 };
        _repo.GetAsync(OrgId, 2025, 2, Arg.Any<CancellationToken>()).Returns(period);

        await _sut.ReopenAsync(OrgId, 2025, 2);

        _repo.Received(1).Remove(period);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_Returns12Months_CorrectClosedFlag()
    {
        var user = new User { FirstName = "Ana", LastName = "López" };
        var closed = new List<AccountingPeriod>
        {
            new() { Year = 2025, Month = 1, ClosedByUserId = UserId, ClosedBy = user, ClosedAtUtc = DateTime.UtcNow }
        };
        _repo.GetClosedForYearAsync(OrgId, 2025, Arg.Any<CancellationToken>()).Returns(closed);

        var result = await _sut.ListAsync(OrgId, 2025);

        result.Should().HaveCount(12);
        result[0].IsClosed.Should().BeTrue();
        result[1].IsClosed.Should().BeFalse();
    }
}
