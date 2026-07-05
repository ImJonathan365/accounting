using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IMemberService
{
    Task<List<MemberDto>> ListAsync(Guid orgId, CancellationToken ct = default);
    Task<MemberDto> InviteAsync(Guid orgId, Guid requesterId, InviteMemberDto dto, CancellationToken ct = default);
    Task<MemberDto> UpdateRoleAsync(Guid orgId, Guid requesterId, Guid targetUserId, UpdateMemberRoleDto dto, CancellationToken ct = default);
    Task<string> RemoveAsync(Guid orgId, Guid requesterId, Guid targetUserId, CancellationToken ct = default);
}

public class MemberService : IMemberService
{
    private readonly IOrganizationRepository _orgs;
    private readonly IUserRepository         _users;
    private readonly IValidator<InviteMemberDto>      _inviteValidator;
    private readonly IValidator<UpdateMemberRoleDto>  _roleValidator;

    public MemberService(
        IOrganizationRepository orgs,
        IUserRepository users,
        IValidator<InviteMemberDto> inviteValidator,
        IValidator<UpdateMemberRoleDto> roleValidator)
    {
        _orgs            = orgs;
        _users           = users;
        _inviteValidator = inviteValidator;
        _roleValidator   = roleValidator;
    }

    public async Task<List<MemberDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var members = await _orgs.GetMembersWithUsersAsync(orgId, ct);
        return members.Select(Map).ToList();
    }

    public async Task<MemberDto> InviteAsync(
        Guid orgId, Guid requesterId, InviteMemberDto dto, CancellationToken ct = default)
    {
        await _inviteValidator.ValidateAndThrowAsync(dto, ct);

        var requesterRole = await _orgs.GetMemberRoleAsync(orgId, requesterId, ct);
        if (!CanInvite(requesterRole))
            throw new UnauthorizedAccessException("Solo los propietarios y administradores pueden invitar miembros.");

        var target = await _users.GetByEmailAsync(dto.Email.ToLowerInvariant(), ct)
            ?? throw new KeyNotFoundException("No se encontró un usuario con ese email. El usuario debe registrarse primero.");

        if (await _orgs.IsOrgMemberAsync(orgId, target.Id, ct))
            throw new InvalidOperationException("El usuario ya es miembro de esta organización.");

        var membership = new Membership
        {
            UserId         = target.Id,
            OrganizationId = orgId,
            Role           = dto.Role
        };

        await _orgs.AddMembershipAsync(membership, ct);
        await _orgs.SaveChangesAsync(ct);

        membership.User = target;
        return Map(membership);
    }

    public async Task<MemberDto> UpdateRoleAsync(
        Guid orgId, Guid requesterId, Guid targetUserId, UpdateMemberRoleDto dto, CancellationToken ct = default)
    {
        await _roleValidator.ValidateAndThrowAsync(dto, ct);

        var requesterRole = await _orgs.GetMemberRoleAsync(orgId, requesterId, ct);
        if (requesterRole != "owner")
            throw new UnauthorizedAccessException("Solo el propietario puede cambiar roles.");

        if (requesterId == targetUserId)
            throw new InvalidOperationException("No puedes cambiar tu propio rol.");

        var membership = await _orgs.GetMemberTrackedAsync(orgId, targetUserId, ct)
            ?? throw new KeyNotFoundException("El miembro no fue encontrado.");

        membership.Role = dto.Role;
        await _orgs.SaveChangesAsync(ct);

        var user = await _users.GetByIdAsync(targetUserId, ct)!;
        membership.User = user!;
        return Map(membership);
    }

    public async Task<string> RemoveAsync(
        Guid orgId, Guid requesterId, Guid targetUserId, CancellationToken ct = default)
    {
        if (requesterId == targetUserId)
            throw new InvalidOperationException("No puedes eliminarte a ti mismo de la organización.");

        var requesterRole = await _orgs.GetMemberRoleAsync(orgId, requesterId, ct);
        var membership    = await _orgs.GetMemberTrackedAsync(orgId, targetUserId, ct)
            ?? throw new KeyNotFoundException("El miembro no fue encontrado.");

        if (!CanRemove(requesterRole, membership.Role))
            throw new UnauthorizedAccessException("No tienes permisos para eliminar este miembro.");

        if (membership.Role == "owner")
        {
            var allMembers = await _orgs.GetMembersWithUsersAsync(orgId, ct);
            if (allMembers.Count(m => m.Role == "owner") <= 1)
                throw new InvalidOperationException("No puedes eliminar al único propietario de la organización.");
        }

        var user = await _users.GetByIdAsync(targetUserId, ct);
        _orgs.RemoveMembership(membership);
        await _orgs.SaveChangesAsync(ct);
        return user?.Email ?? targetUserId.ToString();
    }

    private static bool CanInvite(string? role)  => role is "owner" or "admin";
    private static bool CanRemove(string? requesterRole, string targetRole) =>
        requesterRole == "owner" || (requesterRole == "admin" && targetRole == "member");

    private static int Rank(string? role) => role switch
    {
        "owner"  => 3,
        "admin"  => 2,
        "member" => 1,
        _        => 0
    };

    private static MemberDto Map(Membership m) =>
        new(m.UserId, m.User.Email, m.User.FirstName, m.User.LastName,
            m.User.AvatarUrl, m.Role, m.CreatedAtUtc);
}
