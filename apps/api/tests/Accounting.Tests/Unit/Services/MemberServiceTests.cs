using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class MemberServiceTests
{
    private readonly IOrganizationRepository         _orgs        = Substitute.For<IOrganizationRepository>();
    private readonly IUserRepository                 _users       = Substitute.For<IUserRepository>();
    private readonly IMemberInvitationRepository     _invitations = Substitute.For<IMemberInvitationRepository>();
    private readonly IEmailNotificationService       _email       = Substitute.For<IEmailNotificationService>();
    private readonly IValidator<InviteMemberDto>     _inviteV     = Substitute.For<IValidator<InviteMemberDto>>();
    private readonly IValidator<UpdateMemberRoleDto> _roleV       = Substitute.For<IValidator<UpdateMemberRoleDto>>();
    private readonly MemberService _sut;

    private static readonly Guid OrgId     = Guid.NewGuid();
    private static readonly Guid OwnerId   = Guid.NewGuid();
    private static readonly Guid AdminId   = Guid.NewGuid();
    private static readonly Guid MemberId  = Guid.NewGuid();
    private static readonly Guid NewUserId = Guid.NewGuid();

    public MemberServiceTests()
    {
        _sut = new MemberService(_orgs, _users, _invitations, _email, _inviteV, _roleV, NullLogger<MemberService>.Instance);

        _inviteV.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _roleV.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    }

    // ── InviteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InviteAsync_OwnerInvitesNewUser_CreatesInvitation()
    {
        _orgs.GetMemberRoleAsync(OrgId, OwnerId, Arg.Any<CancellationToken>()).Returns("owner");
        _users.GetByEmailAsync("new@test.com", Arg.Any<CancellationToken>())
            .Returns(MakeUser(NewUserId, "new@test.com"));
        _orgs.IsOrgMemberAsync(OrgId, NewUserId, Arg.Any<CancellationToken>()).Returns(false);
        _invitations.GetPendingAsync(OrgId, "new@test.com", Arg.Any<CancellationToken>())
            .Returns((MemberInvitation?)null);

        await _sut.InviteAsync(OrgId, OwnerId, new InviteMemberDto("new@test.com", "member"));

        await _invitations.Received(1).AddAsync(Arg.Any<MemberInvitation>(), Arg.Any<CancellationToken>());
        await _invitations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InviteAsync_AdminInvitesNewUser_Succeeds()
    {
        _orgs.GetMemberRoleAsync(OrgId, AdminId, Arg.Any<CancellationToken>()).Returns("admin");
        _users.GetByEmailAsync("new@test.com", Arg.Any<CancellationToken>())
            .Returns(MakeUser(NewUserId, "new@test.com"));
        _orgs.IsOrgMemberAsync(OrgId, NewUserId, Arg.Any<CancellationToken>()).Returns(false);
        _invitations.GetPendingAsync(OrgId, "new@test.com", Arg.Any<CancellationToken>())
            .Returns((MemberInvitation?)null);

        await _sut.InviteAsync(OrgId, AdminId, new InviteMemberDto("new@test.com", "member"));

        await _invitations.Received(1).AddAsync(Arg.Any<MemberInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InviteAsync_MemberRole_ThrowsUnauthorized()
    {
        _orgs.GetMemberRoleAsync(OrgId, MemberId, Arg.Any<CancellationToken>()).Returns("member");

        await _sut.Invoking(s => s.InviteAsync(OrgId, MemberId, new InviteMemberDto("x@test.com", "member")))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*propietarios y administradores*");
    }

    [Fact]
    public async Task InviteAsync_UserNotRegistered_ThrowsKeyNotFound()
    {
        _orgs.GetMemberRoleAsync(OrgId, OwnerId, Arg.Any<CancellationToken>()).Returns("owner");
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await _sut.Invoking(s => s.InviteAsync(OrgId, OwnerId, new InviteMemberDto("ghost@test.com", "member")))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*registrarse primero*");
    }

    [Fact]
    public async Task InviteAsync_UserAlreadyMember_ThrowsInvalidOperation()
    {
        _orgs.GetMemberRoleAsync(OrgId, OwnerId, Arg.Any<CancellationToken>()).Returns("owner");
        _users.GetByEmailAsync("existing@test.com", Arg.Any<CancellationToken>())
            .Returns(MakeUser(MemberId, "existing@test.com"));
        _orgs.IsOrgMemberAsync(OrgId, MemberId, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.Invoking(s => s.InviteAsync(OrgId, OwnerId, new InviteMemberDto("existing@test.com", "member")))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya es miembro*");
    }

    // ── UpdateRoleAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoleAsync_OwnerChangesAdminToMember_Succeeds()
    {
        var membership = MakeMembership(MemberId, OrgId, "admin");
        _orgs.GetMemberRoleAsync(OrgId, OwnerId, Arg.Any<CancellationToken>()).Returns("owner");
        _orgs.GetMemberTrackedAsync(OrgId, MemberId, Arg.Any<CancellationToken>()).Returns(membership);
        _users.GetByIdAsync(MemberId, Arg.Any<CancellationToken>())
            .Returns(MakeUser(MemberId, "admin@test.com"));

        var result = await _sut.UpdateRoleAsync(OrgId, OwnerId, MemberId, new UpdateMemberRoleDto("member"));

        result.Role.Should().Be("member");
        membership.Role.Should().Be("member");
    }

    [Fact]
    public async Task UpdateRoleAsync_NonOwnerRequester_ThrowsUnauthorized()
    {
        _orgs.GetMemberRoleAsync(OrgId, AdminId, Arg.Any<CancellationToken>()).Returns("admin");

        await _sut.Invoking(s => s.UpdateRoleAsync(OrgId, AdminId, MemberId, new UpdateMemberRoleDto("member")))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*propietario*");
    }

    [Fact]
    public async Task UpdateRoleAsync_OwnerChangesOwnRole_ThrowsInvalidOperation()
    {
        _orgs.GetMemberRoleAsync(OrgId, OwnerId, Arg.Any<CancellationToken>()).Returns("owner");

        await _sut.Invoking(s => s.UpdateRoleAsync(OrgId, OwnerId, OwnerId, new UpdateMemberRoleDto("member")))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*propio rol*");
    }

    // ── RemoveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_OwnerRemovesMember_ReturnsEmail()
    {
        var user       = MakeUser(MemberId, "member@test.com");
        var membership = MakeMembership(MemberId, OrgId, "member");
        _orgs.GetMemberRoleAsync(OrgId, OwnerId, Arg.Any<CancellationToken>()).Returns("owner");
        _orgs.GetMemberTrackedAsync(OrgId, MemberId, Arg.Any<CancellationToken>()).Returns(membership);
        _users.GetByIdAsync(MemberId, Arg.Any<CancellationToken>()).Returns(user);

        var email = await _sut.RemoveAsync(OrgId, OwnerId, MemberId);

        email.Should().Be("member@test.com");
        _orgs.Received(1).RemoveMembership(membership);
    }

    [Fact]
    public async Task RemoveAsync_AdminRemovesMember_Succeeds()
    {
        var user       = MakeUser(MemberId, "member@test.com");
        var membership = MakeMembership(MemberId, OrgId, "member");
        _orgs.GetMemberRoleAsync(OrgId, AdminId, Arg.Any<CancellationToken>()).Returns("admin");
        _orgs.GetMemberTrackedAsync(OrgId, MemberId, Arg.Any<CancellationToken>()).Returns(membership);
        _users.GetByIdAsync(MemberId, Arg.Any<CancellationToken>()).Returns(user);

        var email = await _sut.RemoveAsync(OrgId, AdminId, MemberId);

        email.Should().Be("member@test.com");
    }

    [Fact]
    public async Task RemoveAsync_AdminRemovesAdmin_ThrowsUnauthorized()
    {
        var targetAdminId = Guid.NewGuid();
        var membership    = MakeMembership(targetAdminId, OrgId, "admin");
        _orgs.GetMemberRoleAsync(OrgId, AdminId, Arg.Any<CancellationToken>()).Returns("admin");
        _orgs.GetMemberTrackedAsync(OrgId, targetAdminId, Arg.Any<CancellationToken>()).Returns(membership);

        await _sut.Invoking(s => s.RemoveAsync(OrgId, AdminId, targetAdminId))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RemoveAsync_RemoveSelf_ThrowsInvalidOperation()
    {
        await _sut.Invoking(s => s.RemoveAsync(OrgId, OwnerId, OwnerId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ti mismo*");
    }

    [Fact]
    public async Task RemoveAsync_LastOwner_ThrowsInvalidOperation()
    {
        var targetId         = Guid.NewGuid();
        var targetMembership = MakeMembership(targetId, OrgId, "owner");

        _orgs.GetMemberRoleAsync(OrgId, OwnerId, Arg.Any<CancellationToken>()).Returns("owner");
        _orgs.GetMemberTrackedAsync(OrgId, targetId, Arg.Any<CancellationToken>()).Returns(targetMembership);
        _orgs.GetMembersWithUsersAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<Membership> { targetMembership });

        await _sut.Invoking(s => s.RemoveAsync(OrgId, OwnerId, targetId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*único propietario*");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static User MakeUser(Guid id, string email) =>
        new() { Id = id, Email = email, FirstName = "Test", LastName = "User" };

    private static Membership MakeMembership(Guid userId, Guid orgId, string role)
    {
        var user = new User { Id = userId, Email = $"{userId}@test.com", FirstName = "Test", LastName = "User" };
        return new Membership { UserId = userId, OrganizationId = orgId, Role = role, User = user };
    }
}
