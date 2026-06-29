namespace Accounting.Application.DTOs;

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string OrganizationName);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserDto User,
    OrganizationDto Organization);

public record UserDto(Guid Id, string Email, string FirstName, string LastName);

public record OrganizationDto(Guid Id, string Name, string Role);
