using System.Security.Claims;
using Accounting.Application.DTOs;
using Accounting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(dto, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("switch-org")]
    public async Task<ActionResult<AuthResponseDto>> SwitchOrg(
        [FromBody] SwitchOrgDto dto, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue("sub")!);
        var result = await _auth.SwitchOrgAsync(userId, dto.OrgId, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        [FromBody] RefreshDto dto, CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(dto.RefreshToken, ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshDto dto, CancellationToken ct)
    {
        await _auth.RevokeAsync(dto.RefreshToken, ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(dto, ct);
        return Ok(new { message = "Si el email está registrado, recibirás un enlace para restablecer tu contraseña." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(dto, ct);
        return Ok(new { message = "Contraseña actualizada correctamente. Ya puedes iniciar sesión." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailDto dto, CancellationToken ct)
    {
        await _auth.VerifyEmailAsync(dto, ct);
        return Ok(new { message = "Email verificado correctamente." });
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _auth.ResendVerificationAsync(dto, ct);
        return Ok(new { message = "Si el email está registrado y no verificado, recibirás un nuevo enlace." });
    }
}
