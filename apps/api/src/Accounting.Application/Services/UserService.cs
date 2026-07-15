using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Enums;
using Accounting.Domain.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Accounting.Application.Services;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task DeleteAccountAsync(Guid userId, DeleteAccountDto dto, CancellationToken ct = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository                 _users;
    private readonly IOrganizationRepository         _orgs;
    private readonly IExternalLoginRepository        _externalLogins;
    private readonly IRefreshTokenRepository         _refreshTokens;
    private readonly IEmailNotificationService       _email;
    private readonly IValidator<UpdateProfileDto>    _updateValidator;
    private readonly IValidator<DeleteAccountDto>    _deleteValidator;
    private readonly IValidator<ChangePasswordDto>   _changePasswordValidator;
    private readonly ILogger<UserService>            _logger;

    public UserService(
        IUserRepository                users,
        IOrganizationRepository        orgs,
        IExternalLoginRepository       externalLogins,
        IRefreshTokenRepository        refreshTokens,
        IEmailNotificationService      email,
        IValidator<UpdateProfileDto>   updateValidator,
        IValidator<DeleteAccountDto>   deleteValidator,
        IValidator<ChangePasswordDto>  changePasswordValidator,
        ILogger<UserService>           logger)
    {
        _users                   = users;
        _orgs                    = orgs;
        _externalLogins          = externalLogins;
        _refreshTokens           = refreshTokens;
        _email                   = email;
        _updateValidator         = updateValidator;
        _deleteValidator         = deleteValidator;
        _changePasswordValidator = changePasswordValidator;
        _logger                  = logger;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");
        return ToDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, ct);

        var user = await _users.GetForUpdateAsync(userId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _users.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        await _changePasswordValidator.ValidateAndThrowAsync(dto, ct);

        var user = await _users.GetForUpdateAsync(userId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var login = await _externalLogins.GetByProviderAsync(AuthProvider.Email, user.Email, ct)
            ?? throw new InvalidOperationException("Cuenta no encontrada.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, login.PasswordHash))
            throw new AuthenticationException("La contraseña actual es incorrecta.");

        login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.SecurityStamp = Guid.NewGuid();

        await _refreshTokens.RevokeAllForUserAsync(userId, ct);
        await _users.SaveChangesAsync(ct);

        FireAndForget(_email.SendPasswordChangedAsync(user.Email, user.FirstName));
    }

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountDto dto, CancellationToken ct = default)
    {
        await _deleteValidator.ValidateAndThrowAsync(dto, ct);

        var user = await _users.GetForUpdateAsync(userId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var login = await _externalLogins.GetByProviderAsync(AuthProvider.Email, user.Email, ct)
            ?? throw new InvalidOperationException("Cuenta no encontrada.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, login.PasswordHash))
            throw new AuthenticationException("La contraseña es incorrecta.");

        // Block if the user is the sole owner of any organization
        var memberships = await _orgs.GetAllMembershipsForUserAsync(userId, ct);
        foreach (var m in memberships.Where(m => m.Role == "owner"))
        {
            var orgMembers = await _orgs.GetMembersWithUsersAsync(m.OrganizationId, ct);
            if (orgMembers.Count(x => x.Role == "owner") <= 1)
                throw new InvalidOperationException(
                    $"Eres el único propietario de \"{m.Organization?.Name}\". Transfiere la propiedad antes de eliminar tu cuenta.");
        }

        user.IsActive      = false;
        user.SecurityStamp = Guid.NewGuid();

        await _refreshTokens.RevokeAllForUserAsync(userId, ct);
        await _users.SaveChangesAsync(ct);
    }

    private void FireAndForget(Task task) =>
        task.ContinueWith(t => _logger.LogError(t.Exception, "Fire-and-forget email failed"),
            TaskContinuationOptions.OnlyOnFaulted);

    private static UserProfileDto ToDto(Domain.Entities.User u) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.AvatarUrl, u.IsActive, u.CreatedAtUtc);
}
