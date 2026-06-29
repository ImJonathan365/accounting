using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using BCrypt.Net;

namespace Accounting.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IOrganizationRepository _orgs;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, IOrganizationRepository orgs, ITokenService tokens)
    {
        _users = users;
        _orgs = orgs;
        _tokens = tokens;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await _users.EmailExistsAsync(dto.Email, ct))
            throw new InvalidOperationException("El email ya está registrado.");

        var user = new User
        {
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        var org = new Organization { Name = dto.OrganizationName };

        var membership = new Membership
        {
            User = user,
            Organization = org,
            Role = "owner"
        };

        await _users.AddAsync(user, ct);
        await _orgs.AddAsync(org, ct);
        await _orgs.AddMembershipAsync(membership, ct);
        await _users.SaveChangesAsync(ct);

        var token = _tokens.GenerateToken(user, org.Id, membership.Role);
        return BuildResponse(token, user, org, membership.Role);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(dto.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

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
