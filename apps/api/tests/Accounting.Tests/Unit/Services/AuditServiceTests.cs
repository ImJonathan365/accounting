using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class AuditServiceTests
{
    private readonly IAuditRepository _repo = Substitute.For<IAuditRepository>();
    private readonly AuditService     _sut;

    private static readonly Guid OrgId    = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();
    private static readonly Guid EntityId = Guid.NewGuid();

    public AuditServiceTests()
    {
        _sut = new AuditService(_repo);
    }

    // ── LogAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task LogAsync_CreatesAuditEntry_WithCorrectFields()
    {
        AuditLog? captured = null;
        await _repo.AddAsync(Arg.Do<AuditLog>(l => captured = l), Arg.Any<CancellationToken>());

        await _sut.LogAsync(OrgId, UserId, "journal.posted", "JournalEntry", EntityId, "Detalle");

        captured.Should().NotBeNull();
        captured!.OrganizationId.Should().Be(OrgId);
        captured.UserId.Should().Be(UserId);
        captured.Action.Should().Be("journal.posted");
        captured.EntityType.Should().Be("JournalEntry");
        captured.EntityId.Should().Be(EntityId);
        captured.Details.Should().Be("Detalle");
    }

    [Fact]
    public async Task LogAsync_CallsSaveChanges()
    {
        await _sut.LogAsync(OrgId, UserId, "account.created", "Account", EntityId);

        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos_WithCorrectPagination()
    {
        var user = new User { Id = UserId, FirstName = "Ana", LastName = "López" };
        var logs = new List<AuditLog>
        {
            new() { Id = Guid.NewGuid(), OrganizationId = OrgId, UserId = UserId, User = user,
                    Action = "journal.posted", EntityType = "JournalEntry", EntityId = EntityId,
                    Details = "Asiento posteado" },
            new() { Id = Guid.NewGuid(), OrganizationId = OrgId, UserId = UserId, User = user,
                    Action = "account.created", EntityType = "Account", EntityId = Guid.NewGuid() },
        };

        _repo.GetPagedAsync(OrgId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((logs, 25));

        var result = await _sut.ListAsync(OrgId, page: 1, pageSize: 10);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(25);
        result.TotalPages.Should().Be(3);  // ceil(25/10)
        result.Items[0].Action.Should().Be("journal.posted");
        result.Items[0].UserName.Should().Be("Ana López");
    }

    [Fact]
    public async Task ListAsync_UserName_IsUnknown_WhenUserIsNull()
    {
        var logs = new List<AuditLog>
        {
            new() { Id = Guid.NewGuid(), OrganizationId = OrgId, UserId = UserId, User = null,
                    Action = "member.removed", EntityType = "Member", EntityId = EntityId },
        };

        _repo.GetPagedAsync(OrgId, 1, 50, Arg.Any<CancellationToken>())
            .Returns((logs, 1));

        var result = await _sut.ListAsync(OrgId, page: 1, pageSize: 50);

        result.Items[0].UserName.Should().Be("Usuario desconocido");
    }
}
