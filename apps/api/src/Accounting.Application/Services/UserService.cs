using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IValidator<UpdateProfileDto> _updateValidator;

    public UserService(IUserRepository users, IValidator<UpdateProfileDto> updateValidator)
    {
        _users = users;
        _updateValidator = updateValidator;
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

    private static UserProfileDto ToDto(Domain.Entities.User u) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.AvatarUrl, u.IsActive, u.CreatedAtUtc);
}
