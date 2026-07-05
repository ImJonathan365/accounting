using System.Security.Claims;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService  _users;
    private readonly IAuthService  _auth;

    public UserController(IUserService users, IAuthService auth)
    {
        _users = users;
        _auth  = auth;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => Ok(await _users.GetProfileAsync(CurrentUserId, ct));

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
        => Ok(await _users.UpdateProfileAsync(CurrentUserId, dto, ct));

    [HttpGet("me/organizations")]
    public async Task<ActionResult<List<UserOrgDto>>> ListOrganizations(CancellationToken ct)
        => Ok(await _auth.ListUserOrgsAsync(CurrentUserId, ct));
}
