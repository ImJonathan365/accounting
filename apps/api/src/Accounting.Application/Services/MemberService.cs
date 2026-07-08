using System.Security.Cryptography;
using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IMemberService
{
    Task<List<MemberDto>>   ListAsync(Guid orgId, CancellationToken ct = default);
    Task                    InviteAsync(Guid orgId, Guid requesterId, InviteMemberDto dto, CancellationToken ct = default);
    Task<InvitationInfoDto> GetInvitationInfoAsync(string rawToken, CancellationToken ct = default);
    Task<MemberDto>         AcceptInvitationAsync(string rawToken, Guid acceptingUserId, CancellationToken ct = default);
    Task                    DeclineInvitationAsync(string rawToken, CancellationToken ct = default);
    Task<MemberDto>         UpdateRoleAsync(Guid orgId, Guid requesterId, Guid targetUserId, UpdateMemberRoleDto dto, CancellationToken ct = default);
    Task<string>            RemoveAsync(Guid orgId, Guid requesterId, Guid targetUserId, CancellationToken ct = default);
}

public class MemberService : IMemberService
{
    private readonly IOrganizationRepository         _orgs;
    private readonly IUserRepository                 _users;
    private readonly IMemberInvitationRepository     _invitations;
    private readonly IEmailNotificationService       _email;
    private readonly IValidator<InviteMemberDto>     _inviteValidator;
    private readonly IValidator<UpdateMemberRoleDto> _roleValidator;

    private const int InvitationExpiryDays = 7;

    public MemberService(
        IOrganizationRepository         orgs,
        IUserRepository                 users,
        IMemberInvitationRepository     invitations,
        IEmailNotificationService       email,
        IValidator<InviteMemberDto>     inviteValidator,
        IValidator<UpdateMemberRoleDto> roleValidator)
    {
        _orgs            = orgs;
        _users           = users;
        _invitations     = invitations;
        _email           = email;
        _inviteValidator = inviteValidator;
        _roleValidator   = roleValidator;
    }

    public async Task<List<MemberDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var members = await _orgs.GetMembersWithUsersAsync(orgId, ct);
        return members.Select(Map).ToList();
    }

    public async Task InviteAsync(
        Guid orgId, Guid requesterId, InviteMemberDto dto, CancellationToken ct = default)
    {
        await _inviteValidator.ValidateAndThrowAsync(dto, ct);

        var requesterRole = await _orgs.GetMemberRoleAsync(orgId, requesterId, ct);
        if (!CanInvite(requesterRole))
            throw new UnauthorizedAccessException("Solo los propietarios y administradores pueden invitar miembros.");

        var email  = dto.Email.ToLowerInvariant();
        var target = await _users.GetByEmailAsync(email, ct)
            ?? throw new KeyNotFoundException("No se encontró un usuario con ese email. El usuario debe registrarse primero.");

        if (await _orgs.IsOrgMemberAsync(orgId, target.Id, ct))
            throw new InvalidOperationException("El usuario ya es miembro de esta organización.");

        // Overwrite any existing pending invitation for this email in this org
        var existing = await _invitations.GetPendingAsync(orgId, email, ct);
        if (existing is not null)
            existing.DeclinedAtUtc = DateTime.UtcNow;

        var raw  = GenerateRaw();
        var hash = Hash(raw);
        var org      = await _orgs.GetByIdAsync(orgId, ct);
        var requester = await _users.GetByIdAsync(requesterId, ct);

        await _invitations.AddAsync(new MemberInvitation
        {
            OrganizationId  = orgId,
            InvitedByUserId = requesterId,
            InvitedEmail    = email,
            Role            = dto.Role,
            TokenHash       = hash,
            ExpiresAtUtc    = DateTime.UtcNow.AddDays(InvitationExpiryDays),
        }, ct);

        await _invitations.SaveChangesAsync(ct);

        var orgName     = org?.Name ?? "";
        var inviterName = requester is not null ? $"{requester.FirstName} {requester.LastName}" : "";

        _ = _email.SendInviteAsync(target.Email, target.FirstName, orgName, inviterName, dto.Role, raw);
    }

    public async Task<InvitationInfoDto> GetInvitationInfoAsync(string rawToken, CancellationToken ct = default)
    {
        var hash       = Hash(rawToken);
        var invitation = await _invitations.GetByTokenHashAsync(hash, ct);

        if (invitation is null)
            return new InvitationInfoDto(Guid.Empty, "", "", "", "", false, "La invitación no existe o el enlace es inválido.");

        if (!invitation.IsPending)
        {
            var reason = invitation.AcceptedAtUtc is not null ? "La invitación ya fue aceptada."
                       : invitation.DeclinedAtUtc is not null ? "La invitación fue rechazada."
                                                              : "La invitación ha expirado.";
            return new InvitationInfoDto(Guid.Empty, "", "", invitation.InvitedEmail, invitation.Role, false, reason);
        }

        var org     = await _orgs.GetByIdAsync(invitation.OrganizationId, ct);
        var inviter = await _users.GetByIdAsync(invitation.InvitedByUserId, ct);

        return new InvitationInfoDto(
            invitation.OrganizationId,
            org?.Name ?? "",
            inviter is not null ? $"{inviter.FirstName} {inviter.LastName}" : "",
            invitation.InvitedEmail,
            invitation.Role,
            true,
            "");
    }

    public async Task<MemberDto> AcceptInvitationAsync(string rawToken, Guid acceptingUserId, CancellationToken ct = default)
    {
        var hash       = Hash(rawToken);
        var invitation = await _invitations.GetByTokenHashAsync(hash, ct)
            ?? throw new KeyNotFoundException("La invitación no existe o el enlace es inválido.");

        if (!invitation.IsPending)
        {
            if (invitation.AcceptedAtUtc is not null) throw new InvalidOperationException("La invitación ya fue aceptada.");
            if (invitation.DeclinedAtUtc is not null) throw new InvalidOperationException("La invitación fue rechazada.");
            throw new InvalidOperationException("La invitación ha expirado.");
        }

        var user = await _users.GetByIdAsync(acceptingUserId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Esta invitación fue enviada a otra dirección de correo.");

        if (await _orgs.IsOrgMemberAsync(invitation.OrganizationId, acceptingUserId, ct))
            throw new InvalidOperationException("Ya eres miembro de esta organización.");

        var membership = new Membership
        {
            UserId         = acceptingUserId,
            OrganizationId = invitation.OrganizationId,
            Role           = invitation.Role,
        };

        await _orgs.AddMembershipAsync(membership, ct);
        invitation.AcceptedAtUtc = DateTime.UtcNow;
        await _invitations.SaveChangesAsync(ct);

        var org = await _orgs.GetByIdAsync(invitation.OrganizationId, ct);
        _ = _email.SendInvitationAcceptedAsync(user.Email, user.FirstName, org?.Name ?? "", invitation.Role);

        membership.User = user;
        return Map(membership);
    }

    public async Task DeclineInvitationAsync(string rawToken, CancellationToken ct = default)
    {
        var hash       = Hash(rawToken);
        var invitation = await _invitations.GetByTokenHashAsync(hash, ct)
            ?? throw new KeyNotFoundException("La invitación no existe o el enlace es inválido.");

        if (!invitation.IsPending)
        {
            if (invitation.AcceptedAtUtc is not null) throw new InvalidOperationException("La invitación ya fue aceptada.");
            if (invitation.DeclinedAtUtc is not null) throw new InvalidOperationException("La invitación ya fue rechazada.");
            throw new InvalidOperationException("La invitación ha expirado.");
        }

        invitation.DeclinedAtUtc = DateTime.UtcNow;
        await _invitations.SaveChangesAsync(ct);
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

    private static string GenerateRaw()
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        // URL-safe base64: +→- /→_ no padding — safe in URL paths without encoding
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string Hash(string raw)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    private static MemberDto Map(Membership m) =>
        new(m.UserId, m.User.Email, m.User.FirstName, m.User.LastName,
            m.User.AvatarUrl, m.Role, m.CreatedAtUtc);
}
