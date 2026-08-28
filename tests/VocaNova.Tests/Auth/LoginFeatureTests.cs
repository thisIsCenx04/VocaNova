using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.Contracts.Requests;
using VocaNova.API.Features.Auth.Contracts.Responses;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.DAL.Repositories;
using VocaNova.API.Features.Auth.BLL.Services;
using VocaNova.API.Features.Auth.Contracts.Requests;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Auth;

public class LoginFeatureTests
{
    [Fact]
    public async Task LoginAsync_Should_Return_Tokens_And_Persist_RefreshToken_When_Credentials_Are_Valid()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, UserStatus.Active);
        var service = CreateAuthService(dbContext);

        var result = await service.LoginAsync(
            new LoginRequest("0912345678", "Password1"),
            deviceInfo: "xunit",
            ipAddress: "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.ExpiresIn.Should().Be(900);

        var user = await dbContext.Users.SingleAsync();
        user.LastLoginAt.Should().NotBeNull();

        var refreshToken = await dbContext.RefreshTokens.SingleAsync();
        refreshToken.UserId.Should().Be(user.UserId);
        refreshToken.TokenHash.Should().Be(TokenHelper.HashSha256(result.Value.RefreshToken));
        refreshToken.DeviceInfo.Should().Be("xunit");
        refreshToken.IpAddress.Should().Be("127.0.0.1");

        var principal = CreateJwtTokenService().ValidateAccessToken(result.Value.AccessToken);
        principal.Should().NotBeNull();
        principal!.FindFirst("user_id")!.Value.Should().Be(user.UserId.ToString());
        principal.FindFirst("role")!.Value.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_401_When_Password_Is_Wrong()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, UserStatus.Active);
        var service = CreateAuthService(dbContext);

        var result = await service.LoginAsync(new LoginRequest("0912345678", "WrongPassword1"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_403_When_User_Is_Locked()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, UserStatus.Locked);
        var service = CreateAuthService(dbContext);

        var result = await service.LoginAsync(new LoginRequest("0912345678", "Password1"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.Error.Should().Be("User account is locked.");
    }

    [Fact]
    public async Task LoginAsync_Should_Return_401_When_User_Is_Deleted()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, UserStatus.Deleted);
        var service = CreateAuthService(dbContext);

        var result = await service.LoginAsync(new LoginRequest("0912345678", "Password1"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void LoginRequestValidator_Should_Reject_Invalid_Phone()
    {
        var validator = new LoginRequestValidator();

        var result = validator.Validate(new LoginRequest("0212345678", "Password1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginRequest.Phone));
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static AuthService CreateAuthService(VocaNovaDbContext dbContext)
    {
        return AuthTestFactory.CreateService(dbContext, googleIdentityProvider: new FakeGoogleTokenVerifier(null));
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        return new JwtTokenService(Options.Create(CreateJwtSettings()));
    }

    private static JwtSettings CreateJwtSettings()
    {
        return new JwtSettings
        {
            Issuer = "VocaNova.Tests",
            Audience = "VocaNova.Tests.Clients",
            SecretKey = "THIS_IS_A_TEST_SECRET_KEY_WITH_32_CHARS_MIN",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        };
    }

    private static async Task SeedUserAsync(VocaNovaDbContext dbContext, string status)
    {
        var role = new Role
        {
            RoleId = 1,
            RoleName = UserRole.User,
        };

        dbContext.Roles.Add(role);
        dbContext.Users.Add(new User
        {
            UserId = 1,
            RoleId = role.RoleId,
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserAuth = new UserAuth
            {
                UserId = 1,
                Phone = "0912345678",
                PasswordHash = PasswordHelper.Hash("Password1"),
                UpdatedAt = DateTime.UtcNow,
            },
            UserProfile = new EntityUserProfile
            {
                UserId = 1,
                FullName = "Nguyen Van A",
                UpdatedAt = DateTime.UtcNow,
            },
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        private readonly GoogleUserInfo? _userInfo;

        public FakeGoogleTokenVerifier(GoogleUserInfo? userInfo)
        {
            _userInfo = userInfo;
        }

        public Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_userInfo);
        }
    }
}
