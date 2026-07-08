using System.Security.Cryptography;
using System.Text;
using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> SwitchOrgAsync(Guid userId, Guid orgId, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    Task<List<UserOrgDto>> ListUserOrgsAsync(Guid userId, CancellationToken ct = default);

    // Password recovery
    Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);

    // Email verification
    Task VerifyEmailAsync(VerifyEmailDto dto, CancellationToken ct = default);
    Task ResendVerificationAsync(ForgotPasswordDto dto, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository                   _users;
    private readonly IOrganizationRepository           _orgs;
    private readonly IExternalLoginRepository          _externalLogins;
    private readonly IRefreshTokenRepository           _refreshTokens;
    private readonly IPasswordResetTokenRepository     _passwordResets;
    private readonly IEmailVerificationTokenRepository _emailVerifications;
    private readonly ITokenService                     _tokens;
    private readonly IAccountSeeder                    _accountSeeder;
    private readonly AuthSettings                      _authSettings;
    private readonly EmailServiceSettings              _emailSettings;
    private readonly IEmailNotificationService         _email;
    private readonly IValidator<RegisterDto>           _registerValidator;
    private readonly IValidator<LoginDto>              _loginValidator;
    private readonly IValidator<ForgotPasswordDto>     _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDto>      _resetPasswordValidator;
    private readonly IValidator<VerifyEmailDto>        _verifyEmailValidator;

    public AuthService(
        IUserRepository                   users,
        IOrganizationRepository           orgs,
        IExternalLoginRepository          externalLogins,
        IRefreshTokenRepository           refreshTokens,
        IPasswordResetTokenRepository     passwordResets,
        IEmailVerificationTokenRepository emailVerifications,
        ITokenService                     tokens,
        IAccountSeeder                    accountSeeder,
        AuthSettings                      authSettings,
        EmailServiceSettings              emailSettings,
        IEmailNotificationService         email,
        IValidator<RegisterDto>           registerValidator,
        IValidator<LoginDto>              loginValidator,
        IValidator<ForgotPasswordDto>     forgotPasswordValidator,
        IValidator<ResetPasswordDto>      resetPasswordValidator,
        IValidator<VerifyEmailDto>        verifyEmailValidator)
    {
        _users                   = users;
        _orgs                    = orgs;
        _externalLogins          = externalLogins;
        _refreshTokens           = refreshTokens;
        _passwordResets          = passwordResets;
        _emailVerifications      = emailVerifications;
        _tokens                  = tokens;
        _accountSeeder           = accountSeeder;
        _authSettings            = authSettings;
        _emailSettings           = emailSettings;
        _email                   = email;
        _registerValidator       = registerValidator;
        _loginValidator          = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator  = resetPasswordValidator;
        _verifyEmailValidator    = verifyEmailValidator;
    }

    private int RefreshExpiryDays => _authSettings.RefreshExpiryDays;

    // ── Register ────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        await _registerValidator.ValidateAndThrowAsync(dto, ct);

        if (await _users.EmailExistsAsync(dto.Email, ct))
            throw new InvalidOperationException("El email ya está registrado.");

        var user = new User
        {
            Email     = dto.Email.ToLowerInvariant(),
            FirstName = dto.FirstName.Trim(),
            LastName  = dto.LastName.Trim()
        };

        var emailLogin = new ExternalLogin
        {
            User           = user,
            Provider       = AuthProvider.Email,
            ProviderUserId = dto.Email.ToLowerInvariant(),
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        var org        = new Organization { Name = dto.OrganizationName.Trim() };
        var membership = new Membership { User = user, Organization = org, Role = "owner" };

        await _users.AddAsync(user, ct);
        await _externalLogins.AddAsync(emailLogin, ct);
        await _orgs.AddAsync(org, ct);
        await _orgs.AddMembershipAsync(membership, ct);
        await _accountSeeder.SeedAsync(org.Id, ct);

        var accessToken      = _tokens.GenerateToken(user, org.Id, membership.Role);
        var rawRefresh       = await IssueRefreshTokenAsync(user.Id, org.Id, ct);
        var rawVerification  = await IssueEmailVerificationTokenAsync(user.Id, ct);
        await _users.SaveChangesAsync(ct);

        var verificationUrl = $"{_emailSettings.AppUrl}/verify-email?token={Uri.EscapeDataString(rawVerification)}";

        // Welcome email is sent only after email verification
        _ = _email.SendVerificationEmailAsync(user.Email, user.FirstName, verificationUrl);

        return BuildResponse(accessToken, rawRefresh, user, org, membership.Role);
    }

    // ── Login ────────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        await _loginValidator.ValidateAndThrowAsync(dto, ct);

        var emailLogin = await _externalLogins.GetByProviderAsync(
            AuthProvider.Email, dto.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, emailLogin.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        var user = emailLogin.User;
        if (!user.IsActive)
            throw new UnauthorizedAccessException("La cuenta está desactivada.");

        var membership = await _orgs.GetFirstMembershipAsync(user.Id, ct)
            ?? throw new InvalidOperationException("El usuario no pertenece a ninguna organización.");

        var org = await _orgs.GetByIdAsync(membership.OrganizationId, ct)
            ?? throw new InvalidOperationException("Organización no encontrada.");

        var accessToken = _tokens.GenerateToken(user, org.Id, membership.Role);
        var rawRefresh  = await IssueRefreshTokenAsync(user.Id, org.Id, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return BuildResponse(accessToken, rawRefresh, user, org, membership.Role);
    }

    // ── Switch org ──────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> SwitchOrgAsync(Guid userId, Guid orgId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var role = await _orgs.GetMemberRoleAsync(orgId, userId, ct)
            ?? throw new UnauthorizedAccessException("No tienes acceso a esta organización.");

        var org = await _orgs.GetByIdAsync(orgId, ct)
            ?? throw new KeyNotFoundException("Organización no encontrada.");

        var accessToken = _tokens.GenerateToken(user, org.Id, role);
        var rawRefresh  = await IssueRefreshTokenAsync(user.Id, org.Id, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return BuildResponse(accessToken, rawRefresh, user, org, role);
    }

    // ── Refresh / Revoke ────────────────────────────────────────────────────

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash   = HashToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, ct);

        if (stored is null || stored.IsRevoked || stored.ExpiresAtUtc < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Token de actualización inválido o expirado.");

        stored.IsRevoked = true;

        var user = await _users.GetByIdAsync(stored.UserId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("La cuenta está desactivada.");

        var role = await _orgs.GetMemberRoleAsync(stored.OrgId, user.Id, ct)
            ?? throw new UnauthorizedAccessException("El usuario ya no tiene acceso a esta organización.");

        var org = await _orgs.GetByIdAsync(stored.OrgId, ct)
            ?? throw new KeyNotFoundException("Organización no encontrada.");

        var newAccessToken = _tokens.GenerateToken(user, org.Id, role);
        var newRawRefresh  = await IssueRefreshTokenAsync(user.Id, org.Id, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return BuildResponse(newAccessToken, newRawRefresh, user, org, role);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = HashToken(refreshToken);
        await _refreshTokens.RevokeByHashAsync(hash, ct);
    }

    public async Task<List<UserOrgDto>> ListUserOrgsAsync(Guid userId, CancellationToken ct = default)
    {
        var memberships = await _orgs.GetAllMembershipsForUserAsync(userId, ct);
        return memberships
            .Select(m => new UserOrgDto(m.OrganizationId, m.Organization!.Name, m.Role))
            .ToList();
    }

    // ── Password recovery ───────────────────────────────────────────────────

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        await _forgotPasswordValidator.ValidateAndThrowAsync(dto, ct);

        // Never reveal whether the email exists
        var login = await _externalLogins.GetByProviderAsync(
            AuthProvider.Email, dto.Email.ToLowerInvariant(), ct);
        if (login is null) return;

        var user = login.User;

        // Invalidate any existing reset tokens
        await _passwordResets.DeleteAllForUserAsync(user.Id, ct);

        var rawToken  = GenerateRaw();
        var tokenHash = HashToken(rawToken);
        await _passwordResets.AddAsync(new PasswordResetToken
        {
            UserId       = user.Id,
            TokenHash    = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
        }, ct);
        await _passwordResets.SaveChangesAsync(ct);

        var resetUrl = $"{_emailSettings.AppUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        _ = _email.SendPasswordResetAsync(user.Email, user.FirstName, resetUrl);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        await _resetPasswordValidator.ValidateAndThrowAsync(dto, ct);

        var hash  = HashToken(dto.Token);
        var token = await _passwordResets.GetByHashAsync(hash, ct)
            ?? throw new InvalidOperationException("El enlace de recuperación es inválido o ya fue utilizado.");

        if (!token.IsValid)
            throw new InvalidOperationException("El enlace de recuperación ha expirado. Solicita uno nuevo.");

        var user = await _users.GetByIdAsync(token.UserId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var login = await _externalLogins.GetByProviderAsync(
            AuthProvider.Email, user.Email, ct)
            ?? throw new InvalidOperationException("Inicio de sesión no encontrado.");

        login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        token.UsedAtUtc    = DateTime.UtcNow;

        // Revoke all sessions — the user must log in again with the new password
        await _refreshTokens.RevokeAllForUserAsync(user.Id, ct);
        await _passwordResets.SaveChangesAsync(ct);
    }

    // ── Email verification ──────────────────────────────────────────────────

    public async Task VerifyEmailAsync(VerifyEmailDto dto, CancellationToken ct = default)
    {
        await _verifyEmailValidator.ValidateAndThrowAsync(dto, ct);

        var hash  = HashToken(dto.Token);
        var token = await _emailVerifications.GetByHashAsync(hash, ct)
            ?? throw new InvalidOperationException("El enlace de verificación es inválido.");

        if (token.ExpiresAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("El enlace de verificación ha expirado. Solicita uno nuevo.");

        var user = await _users.GetForUpdateAsync(token.UserId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (user.EmailVerifiedAt.HasValue)
        {
            await _emailVerifications.DeleteAllForUserAsync(user.Id, ct);
            await _emailVerifications.SaveChangesAsync(ct);
            return;
        }

        user.EmailVerifiedAt = DateTime.UtcNow;
        await _emailVerifications.DeleteAllForUserAsync(user.Id, ct);
        await _emailVerifications.SaveChangesAsync(ct);

        // Now that the email is confirmed, send the welcome message
        var membership = await _orgs.GetFirstMembershipAsync(user.Id, ct);
        var org = membership is not null
            ? await _orgs.GetByIdAsync(membership.OrganizationId, ct)
            : null;
        _ = _email.SendWelcomeAsync(user.Email, user.FirstName, org?.Name ?? "");
    }

    public async Task ResendVerificationAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        await _forgotPasswordValidator.ValidateAndThrowAsync(dto, ct);

        var user = await _users.GetByEmailAsync(dto.Email.ToLowerInvariant(), ct);
        if (user is null || user.EmailVerifiedAt.HasValue) return;

        await _emailVerifications.DeleteAllForUserAsync(user.Id, ct);

        var rawToken = await IssueEmailVerificationTokenAsync(user.Id, ct);
        await _emailVerifications.SaveChangesAsync(ct);

        var verificationUrl = $"{_emailSettings.AppUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";
        _ = _email.SendVerificationEmailAsync(user.Email, user.FirstName, verificationUrl);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<string> IssueRefreshTokenAsync(Guid userId, Guid orgId, CancellationToken ct)
    {
        var raw  = GenerateRaw();
        var hash = HashToken(raw);
        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId       = userId,
            OrgId        = orgId,
            TokenHash    = hash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshExpiryDays),
        }, ct);
        return raw;
    }

    private async Task<string> IssueEmailVerificationTokenAsync(Guid userId, CancellationToken ct)
    {
        var raw  = GenerateRaw();
        var hash = HashToken(raw);
        await _emailVerifications.AddAsync(new EmailVerificationToken
        {
            UserId       = userId,
            TokenHash    = hash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24),
        }, ct);
        return raw;
    }

    private AuthResponseDto BuildResponse(
        string accessToken, string refreshToken,
        User user, Organization org, string role) =>
        new(
            AccessToken:      accessToken,
            TokenType:        "Bearer",
            ExpiresIn:        _tokens.ExpirySeconds,
            RefreshToken:     refreshToken,
            RefreshExpiresIn: RefreshExpiryDays * 86400,
            User:             new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.EmailVerifiedAt.HasValue),
            Organization:     new OrganizationDto(org.Id, org.Name, role)
        );

    private static string GenerateRaw()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
