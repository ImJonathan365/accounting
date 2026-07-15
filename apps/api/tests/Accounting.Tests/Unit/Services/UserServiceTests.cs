using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly IUserRepository                  _users         = Substitute.For<IUserRepository>();
    private readonly IOrganizationRepository          _orgs          = Substitute.For<IOrganizationRepository>();
    private readonly IExternalLoginRepository         _externalLogins = Substitute.For<IExternalLoginRepository>();
    private readonly IRefreshTokenRepository          _refreshTokens  = Substitute.For<IRefreshTokenRepository>();
    private readonly IEmailNotificationService        _email          = Substitute.For<IEmailNotificationService>();
    private readonly IValidator<UpdateProfileDto>     _updateValidator      = Substitute.For<IValidator<UpdateProfileDto>>();
    private readonly IValidator<DeleteAccountDto>     _deleteValidator      = Substitute.For<IValidator<DeleteAccountDto>>();
    private readonly IValidator<ChangePasswordDto>    _changePasswordValidator = Substitute.For<IValidator<ChangePasswordDto>>();
    private readonly ILogger<UserService>             _logger         = Substitute.For<ILogger<UserService>>();
    private readonly UserService                      _sut;

    private static readonly Guid OrgId  = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private const string CorrectPassword = "CorrectPass1!";

    public UserServiceTests()
    {
        _sut = new UserService(
            _users, _orgs, _externalLogins, _refreshTokens,
            _email, _updateValidator, _deleteValidator, _changePasswordValidator, _logger);

        _updateValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _deleteValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _changePasswordValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var user  = MakeUser();
        var login = MakeLogin(user.Email, CorrectPassword);

        _users.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _externalLogins.GetByProviderAsync(AuthProvider.Email, user.Email, Arg.Any<CancellationToken>())
            .Returns(login);

        // Default: no org memberships
        _orgs.GetAllMembershipsForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<Membership>());
    }

    // ─── ChangePasswordAsync ──────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsAuthenticationException()
    {
        await _sut.Invoking(s => s.ChangePasswordAsync(UserId,
                new ChangePasswordDto("WrongPass", "NewPass1!", "NewPass1!")))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*contraseña actual*");
    }

    [Fact]
    public async Task ChangePasswordAsync_CorrectPassword_RotatesSecurityStamp()
    {
        var user  = MakeUser();
        var login = MakeLogin(user.Email, CorrectPassword);
        var oldStamp = user.SecurityStamp;

        _users.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _externalLogins.GetByProviderAsync(AuthProvider.Email, user.Email, Arg.Any<CancellationToken>())
            .Returns(login);

        await _sut.ChangePasswordAsync(UserId,
            new ChangePasswordDto(CorrectPassword, "NewPass1!", "NewPass1!"));

        user.SecurityStamp.Should().NotBe(oldStamp);
        await _refreshTokens.Received(1).RevokeAllForUserAsync(UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_HashesNewPassword()
    {
        var login = MakeLogin("test@test.com", CorrectPassword);
        _externalLogins.GetByProviderAsync(AuthProvider.Email, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(login);

        var oldHash = login.PasswordHash;

        await _sut.ChangePasswordAsync(UserId,
            new ChangePasswordDto(CorrectPassword, "NewSecure1!", "NewSecure1!"));

        login.PasswordHash.Should().NotBe(oldHash);
        BCrypt.Net.BCrypt.Verify("NewSecure1!", login.PasswordHash).Should().BeTrue();
    }

    // ─── DeleteAccountAsync ──────────────────────────────────────────

    [Fact]
    public async Task DeleteAccountAsync_WrongPassword_ThrowsAuthenticationException()
    {
        await _sut.Invoking(s => s.DeleteAccountAsync(UserId, new DeleteAccountDto("WrongPass")))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*contraseña*");
    }

    [Fact]
    public async Task DeleteAccountAsync_SoleOwnerOfOrg_ThrowsInvalidOperation()
    {
        var orgId = Guid.NewGuid();
        _orgs.GetAllMembershipsForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<Membership> { new() { OrganizationId = orgId, Role = "owner", Organization = new Organization { Name = "Mi Empresa" } } });
        _orgs.GetMembersWithUsersAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(new List<Membership> { new() { Role = "owner" } }); // only one owner

        await _sut.Invoking(s => s.DeleteAccountAsync(UserId, new DeleteAccountDto(CorrectPassword)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*único propietario*");
    }

    [Fact]
    public async Task DeleteAccountAsync_MultipleOwners_AllowsDeletion()
    {
        var orgId = Guid.NewGuid();
        _orgs.GetAllMembershipsForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<Membership> { new() { OrganizationId = orgId, Role = "owner" } });
        _orgs.GetMembersWithUsersAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(new List<Membership> { new() { Role = "owner" }, new() { Role = "owner" } }); // two owners

        await _sut.Invoking(s => s.DeleteAccountAsync(UserId, new DeleteAccountDto(CorrectPassword)))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAccountAsync_Valid_SoftDeletesAndRotatesSecurityStamp()
    {
        var user     = MakeUser();
        var oldStamp = user.SecurityStamp;
        _users.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _externalLogins.GetByProviderAsync(AuthProvider.Email, user.Email, Arg.Any<CancellationToken>())
            .Returns(MakeLogin(user.Email, CorrectPassword));

        await _sut.DeleteAccountAsync(UserId, new DeleteAccountDto(CorrectPassword));

        user.IsActive.Should().BeFalse();
        user.SecurityStamp.Should().NotBe(oldStamp);
        await _refreshTokens.Received(1).RevokeAllForUserAsync(UserId, Arg.Any<CancellationToken>());
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static User MakeUser() =>
        new() { Id = UserId, Email = "user@test.com", FirstName = "Juan", LastName = "García", SecurityStamp = Guid.NewGuid(), IsActive = true };

    private static ExternalLogin MakeLogin(string email, string plainPassword) =>
        new() { Provider = AuthProvider.Email, ProviderUserId = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword) };
}
