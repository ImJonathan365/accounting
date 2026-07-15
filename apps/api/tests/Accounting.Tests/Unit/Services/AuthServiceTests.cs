using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly IUserRepository                  _users          = Substitute.For<IUserRepository>();
    private readonly IOrganizationRepository          _orgs           = Substitute.For<IOrganizationRepository>();
    private readonly IExternalLoginRepository         _externalLogins = Substitute.For<IExternalLoginRepository>();
    private readonly IRefreshTokenRepository          _refreshTokens  = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordResetTokenRepository    _passwordResets = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IEmailVerificationTokenRepository _emailVerifs   = Substitute.For<IEmailVerificationTokenRepository>();
    private readonly ITokenService                    _tokens         = Substitute.For<ITokenService>();
    private readonly IAccountSeeder                   _seeder         = Substitute.For<IAccountSeeder>();
    private readonly IEmailNotificationService        _email          = Substitute.For<IEmailNotificationService>();
    private readonly IValidator<RegisterDto>          _registerV      = Substitute.For<IValidator<RegisterDto>>();
    private readonly IValidator<LoginDto>             _loginV         = Substitute.For<IValidator<LoginDto>>();
    private readonly IValidator<ForgotPasswordDto>    _forgotV        = Substitute.For<IValidator<ForgotPasswordDto>>();
    private readonly IValidator<ResetPasswordDto>     _resetV         = Substitute.For<IValidator<ResetPasswordDto>>();
    private readonly IValidator<VerifyEmailDto>       _verifyV        = Substitute.For<IValidator<VerifyEmailDto>>();
    private readonly AuthSettings                     _settings       = new() { RefreshExpiryDays = 7 };
    private readonly EmailServiceSettings             _emailSettings  = new() { BaseUrl = "", AppUrl = "http://localhost:3000" };
    private readonly AuthService _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OrgId  = Guid.NewGuid();

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _users, _orgs, _externalLogins, _refreshTokens,
            _passwordResets, _emailVerifs,
            _tokens, _seeder, _settings, _emailSettings,
            _email, _registerV, _loginV, _forgotV, _resetV, _verifyV,
            NullLogger<AuthService>.Instance);

        _registerV.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _loginV.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _tokens.GenerateToken(Arg.Any<User>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("jwt_token");
        _tokens.ExpirySeconds.Returns(3600);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var user       = MakeUser(UserId, "user@test.com");
        var membership = MakeMembership(user, OrgId, "owner");
        var org        = new Organization { Id = OrgId, Name = "Mi Empresa" };
        var login      = MakeEmailLogin(user, "correct_password");

        _externalLogins.GetByProviderAsync(AuthProvider.Email, "user@test.com", Arg.Any<CancellationToken>())
            .Returns(login);
        _orgs.GetFirstMembershipAsync(UserId, Arg.Any<CancellationToken>()).Returns(membership);
        _orgs.GetByIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns(org);

        var result = await _sut.LoginAsync(new LoginDto("user@test.com", "correct_password"));

        result.AccessToken.Should().Be("jwt_token");
        result.User.Email.Should().Be("user@test.com");
        result.Organization.Name.Should().Be("Mi Empresa");
        result.Organization.Role.Should().Be("owner");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshExpiresIn.Should().Be(7 * 86400);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var user  = MakeUser(UserId, "user@test.com");
        var login = MakeEmailLogin(user, "correct_password");

        _externalLogins.GetByProviderAsync(AuthProvider.Email, "user@test.com", Arg.Any<CancellationToken>())
            .Returns(login);

        await _sut.Invoking(s => s.LoginAsync(new LoginDto("user@test.com", "wrong_password")))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Credenciales inválidas*");
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        _externalLogins.GetByProviderAsync(AuthProvider.Email, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ExternalLogin?)null);

        await _sut.Invoking(s => s.LoginAsync(new LoginDto("ghost@test.com", "pass")))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Credenciales inválidas*");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorized()
    {
        var user  = MakeUser(UserId, "user@test.com", isActive: false);
        var login = MakeEmailLogin(user, "correct_password");

        _externalLogins.GetByProviderAsync(AuthProvider.Email, "user@test.com", Arg.Any<CancellationToken>())
            .Returns(login);

        await _sut.Invoking(s => s.LoginAsync(new LoginDto("user@test.com", "correct_password")))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*desactivada*");
    }

    [Fact]
    public async Task LoginAsync_SavesRefreshTokenToRepository()
    {
        var user       = MakeUser(UserId, "user@test.com");
        var membership = MakeMembership(user, OrgId, "owner");
        var org        = new Organization { Id = OrgId, Name = "Empresa" };
        var login      = MakeEmailLogin(user, "pass123");

        _externalLogins.GetByProviderAsync(AuthProvider.Email, "user@test.com", Arg.Any<CancellationToken>())
            .Returns(login);
        _orgs.GetFirstMembershipAsync(UserId, Arg.Any<CancellationToken>()).Returns(membership);
        _orgs.GetByIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns(org);

        await _sut.LoginAsync(new LoginDto("user@test.com", "pass123"));

        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.UserId == UserId && t.OrgId == OrgId && !t.IsRevoked),
            Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewUser_CreatesUserOrgAndReturnsToken()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.RegisterAsync(new RegisterDto(
            "new@test.com", "Password1!", "Juan", "Pérez", "Mi Org"));

        result.AccessToken.Should().Be("jwt_token");
        result.User.FirstName.Should().Be("Juan");
        result.Organization.Role.Should().Be("owner");
        result.RefreshToken.Should().NotBeNullOrEmpty();

        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _orgs.Received(1).AddAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>());
        await _orgs.Received(1).AddMembershipAsync(
            Arg.Is<Membership>(m => m.Role == "owner"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        _users.EmailExistsAsync("taken@test.com", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.Invoking(s => s.RegisterAsync(
                new RegisterDto("taken@test.com", "Pass1!", "A", "B", "OrgX")))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya está registrado*");
    }

    // ── SwitchOrgAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SwitchOrgAsync_ValidMembership_ReturnsNewToken()
    {
        var user = MakeUser(UserId, "user@test.com");
        var org  = new Organization { Id = OrgId, Name = "Segunda Org" };

        _users.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _orgs.GetMemberRoleAsync(OrgId, UserId, Arg.Any<CancellationToken>()).Returns("admin");
        _orgs.GetByIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns(org);

        var result = await _sut.SwitchOrgAsync(UserId, OrgId);

        result.Organization.Role.Should().Be("admin");
        result.Organization.Name.Should().Be("Segunda Org");
    }

    [Fact]
    public async Task SwitchOrgAsync_NotMember_ThrowsUnauthorized()
    {
        var user = MakeUser(UserId, "user@test.com");
        _users.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _orgs.GetMemberRoleAsync(OrgId, UserId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        await _sut.Invoking(s => s.SwitchOrgAsync(UserId, OrgId))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*No tienes acceso*");
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesAndReturnsNewToken()
    {
        var user       = MakeUser(UserId, "user@test.com");
        var org        = new Organization { Id = OrgId, Name = "Org" };
        var rawToken   = "valid_raw_token";
        var storedHash = HashToken(rawToken);
        var stored     = new RefreshToken
        {
            UserId       = UserId,
            OrgId        = OrgId,
            TokenHash    = storedHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(6),
            IsRevoked    = false,
        };

        _refreshTokens.GetByHashAsync(storedHash, Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _orgs.GetMemberRoleAsync(OrgId, UserId, Arg.Any<CancellationToken>()).Returns("owner");
        _orgs.GetByIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns(org);

        var result = await _sut.RefreshAsync(rawToken);

        result.AccessToken.Should().Be("jwt_token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(rawToken);
        stored.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsUnauthorized()
    {
        var rawToken   = "expired_token";
        var storedHash = HashToken(rawToken);
        var stored     = new RefreshToken
        {
            UserId       = UserId,
            OrgId        = OrgId,
            TokenHash    = storedHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // expired
            IsRevoked    = false,
        };

        _refreshTokens.GetByHashAsync(storedHash, Arg.Any<CancellationToken>()).Returns(stored);

        await _sut.Invoking(s => s.RefreshAsync(rawToken))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*inválido o expirado*");
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ThrowsUnauthorized()
    {
        var rawToken   = "revoked_token";
        var storedHash = HashToken(rawToken);
        var stored     = new RefreshToken
        {
            UserId       = UserId,
            OrgId        = OrgId,
            TokenHash    = storedHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(5),
            IsRevoked    = true,
        };

        _refreshTokens.GetByHashAsync(storedHash, Arg.Any<CancellationToken>()).Returns(stored);

        await _sut.Invoking(s => s.RefreshAsync(rawToken))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*inválido o expirado*");
    }

    [Fact]
    public async Task RefreshAsync_TokenNotFound_ThrowsUnauthorized()
    {
        _refreshTokens.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        await _sut.Invoking(s => s.RefreshAsync("nonexistent"))
            .Should().ThrowAsync<AuthenticationException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User MakeUser(Guid id, string email, bool isActive = true) =>
        new() { Id = id, Email = email, FirstName = "Test", LastName = "User", IsActive = isActive };

    private static Membership MakeMembership(User user, Guid orgId, string role) =>
        new() { UserId = user.Id, OrganizationId = orgId, Role = role, User = user };

    private static ExternalLogin MakeEmailLogin(User user, string plainPassword) =>
        new()
        {
            User           = user,
            Provider       = AuthProvider.Email,
            ProviderUserId = user.Email,
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(plainPassword),
        };

    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
