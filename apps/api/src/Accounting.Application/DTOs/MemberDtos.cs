namespace Accounting.Application.DTOs;

public record MemberDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAtUtc);

public record InviteMemberDto(string Email, string Role);

public record UpdateMemberRoleDto(string Role);

public record InvitationInfoDto(
    Guid   OrgId,
    string OrgName,
    string InviterName,
    string InvitedEmail,
    string Role,
    bool   IsValid,
    string InvalidReason);
