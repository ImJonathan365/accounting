using System.Security.Claims;
using Accounting.Api.Filters;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Authorize]
[ServiceFilter(typeof(OrgMembershipFilter))]
[Route("api/organizations/{orgId:guid}/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _service;
    private readonly IAuditService  _audit;

    public MembersController(IMemberService service, IAuditService audit)
    {
        _service = service;
        _audit   = audit;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<MemberDto>>> List(Guid orgId, CancellationToken ct)
        => Ok(await _service.ListAsync(orgId, ct));

    [HttpPost]
    public async Task<ActionResult<MemberDto>> Invite(
        Guid orgId, [FromBody] InviteMemberDto dto, CancellationToken ct)
    {
        var member = await _service.InviteAsync(orgId, CurrentUserId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.MemberInvited, "Member", member.UserId,
            $"Invitó a {member.Email} como {member.Role}", ct);
        return Ok(member);
    }

    [HttpPut("{userId:guid}/role")]
    public async Task<ActionResult<MemberDto>> UpdateRole(
        Guid orgId, Guid userId, [FromBody] UpdateMemberRoleDto dto, CancellationToken ct)
    {
        var member = await _service.UpdateRoleAsync(orgId, CurrentUserId, userId, dto, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.MemberRoleChanged, "Member", userId,
            $"Cambió el rol de {member.Email} a {member.Role}", ct);
        return Ok(member);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid orgId, Guid userId, CancellationToken ct)
    {
        var email = await _service.RemoveAsync(orgId, CurrentUserId, userId, ct);
        await _audit.LogAsync(orgId, CurrentUserId,
            AuditActions.MemberRemoved, "Member", userId,
            $"Eliminó a {email} del equipo", ct);
        return NoContent();
    }
}
