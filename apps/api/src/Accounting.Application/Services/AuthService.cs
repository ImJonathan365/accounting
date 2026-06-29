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

    // TODO: Google OAuth — Task<AuthResponseDto> GoogleSignInAsync(string idToken, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IOrganizationRepository _orgs;
    private readonly IExternalLoginRepository _externalLogins;
    private readonly ITokenService _tokens;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;

    public AuthService(
        IUserRepository users,
        IOrganizationRepository orgs,
        IExternalLoginRepository externalLogins,
        ITokenService tokens,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator)
    {
        _users = users;
        _orgs = orgs;
        _externalLogins = externalLogins;
        _tokens = tokens;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        await _registerValidator.ValidateAndThrowAsync(dto, ct);

        if (await _users.EmailExistsAsync(dto.Email, ct))
            throw new InvalidOperationException("El email ya está registrado.");

        var user = new User
        {
            Email = dto.Email.ToLowerInvariant(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim()
        };

        var emailLogin = new ExternalLogin
        {
            User = user,
            Provider = AuthProvider.Email,
            ProviderUserId = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        var org = new Organization { Name = dto.OrganizationName.Trim() };
        var membership = new Membership { User = user, Organization = org, Role = "owner" };

        await _users.AddAsync(user, ct);
        await _externalLogins.AddAsync(emailLogin, ct);
        await _orgs.AddAsync(org, ct);
        await _orgs.AddMembershipAsync(membership, ct);
        await _users.SaveChangesAsync(ct);

        // TODO: Notifications — await _notificationService.SendWelcomeAsync(user, org);

        var token = _tokens.GenerateToken(user, org.Id, membership.Role);
        return BuildResponse(token, user, org, membership.Role);
    }

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

        var token = _tokens.GenerateToken(user, org.Id, membership.Role);
        return BuildResponse(token, user, org, membership.Role);
    }

    private static AuthResponseDto BuildResponse(string token, User user, Organization org, string role) =>
        new(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: 3600,
            User: new UserDto(user.Id, user.Email, user.FirstName, user.LastName),
            Organization: new OrganizationDto(org.Id, org.Name, role)
        );
}
