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
    private readonly IUserService _users;
    public UserController(IUserService users) => _users = users;

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue("sub")!);
        var profile = await _users.GetProfileAsync(userId, ct);
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue("sub")!);
        var profile = await _users.UpdateProfileAsync(userId, dto, ct);
        return Ok(profile);
    }
}
