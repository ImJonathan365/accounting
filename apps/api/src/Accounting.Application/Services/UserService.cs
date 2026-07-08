using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Enums;
using FluentValidation;

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
    private readonly IExternalLoginRepository        _externalLogins;
    private readonly IRefreshTokenRepository         _refreshTokens;
    private readonly IEmailNotificationService       _email;
    private readonly IValidator<UpdateProfileDto>    _updateValidator;
    private readonly IValidator<DeleteAccountDto>    _deleteValidator;
    private readonly IValidator<ChangePasswordDto>   _changePasswordValidator;

    public UserService(
        IUserRepository                users,
        IExternalLoginRepository       externalLogins,
        IRefreshTokenRepository        refreshTokens,
        IEmailNotificationService      email,
        IValidator<UpdateProfileDto>   updateValidator,
        IValidator<DeleteAccountDto>   deleteValidator,
        IValidator<ChangePasswordDto>  changePasswordValidator)
    {
        _users                   = users;
        _externalLogins          = externalLogins;
        _refreshTokens           = refreshTokens;
        _email                   = email;
        _updateValidator         = updateValidator;
        _deleteValidator         = deleteValidator;
        _changePasswordValidator = changePasswordValidator;
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

        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var login = await _externalLogins.GetByProviderAsync(AuthProvider.Email, user.Email, ct)
            ?? throw new InvalidOperationException("Cuenta no encontrada.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, login.PasswordHash))
            throw new UnauthorizedAccessException("La contraseña actual es incorrecta.");

        login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _users.SaveChangesAsync(ct);

        _ = _email.SendPasswordChangedAsync(user.Email, user.FirstName);
    }

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountDto dto, CancellationToken ct = default)
    {
        await _deleteValidator.ValidateAndThrowAsync(dto, ct);

        var user = await _users.GetForUpdateAsync(userId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var login = await _externalLogins.GetByProviderAsync(AuthProvider.Email, user.Email, ct)
            ?? throw new InvalidOperationException("Cuenta no encontrada.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, login.PasswordHash))
            throw new UnauthorizedAccessException("La contraseña es incorrecta.");

        user.IsActive = false;

        await _refreshTokens.RevokeAllForUserAsync(userId, ct);
        await _users.SaveChangesAsync(ct);
    }

    private static UserProfileDto ToDto(Domain.Entities.User u) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.AvatarUrl, u.IsActive, u.CreatedAtUtc);
}
