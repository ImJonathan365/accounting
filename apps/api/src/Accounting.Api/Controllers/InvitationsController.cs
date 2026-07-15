using System.Security.Claims;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IMemberService _members;
    public InvitationsController(IMemberService members) => _members = members;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpGet("{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GetInfo(string token, CancellationToken ct)
        => Ok(await _members.GetInvitationInfoAsync(token, ct));

    [HttpPost("{token}/accept")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Accept(string token, CancellationToken ct)
    {
        var member = await _members.AcceptInvitationAsync(token, CurrentUserId, ct);
        return Ok(member);
    }

    [HttpPost("{token}/decline")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Decline(string token, CancellationToken ct)
    {
        await _members.DeclineInvitationAsync(token, ct);
        return NoContent();
    }
}
