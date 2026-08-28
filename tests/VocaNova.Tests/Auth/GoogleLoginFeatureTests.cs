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

public class GoogleLoginFeatureTests
{
    [Fact]
    public async Task GoogleLoginAsync_Should_Create_User_When_Google_User_Is_New()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        var googleUser = new GoogleUserInfo("google-uid-1", "new@example.com", true, "New Google User", "https://example.com/avatar.png");
        var service = CreateAuthService(dbContext, googleUser);

        var result = await service.GoogleLoginAsync(
            new GoogleLoginRequest("google-id-token"),
            deviceInfo: "xunit",
            ipAddress: "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var user = await dbContext.Users
            .Include(entity => entity.UserAuth)
            .Include(entity => entity.UserProfile)
            .SingleAsync();
        user.UserAuth!.Phone.Should().BeNull();
        user.UserAuth.GoogleUid.Should().Be("google-uid-1");
        user.UserAuth.GoogleEmail.Should().Be("new@example.com");
        user.UserProfile!.FullName.Should().Be("New Google User");
        user.UserProfile.AvatarUrl.Should().Be("https://example.com/avatar.png");

        var refreshToken = await dbContext.RefreshTokens.SingleAsync();
        refreshToken.UserId.Should().Be(user.UserId);
        refreshToken.TokenHash.Should().Be(TokenHelper.HashSha256(result.Value!.RefreshToken));
        refreshToken.DeviceInfo.Should().Be("xunit");
        refreshToken.IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task GoogleLoginAsync_Should_Login_Existing_Google_User()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedGoogleUserAsync(dbContext, "google-uid-1", "existing@example.com", UserStatus.Active);
        var googleUser = new GoogleUserInfo("google-uid-1", "existing@example.com", true, "Existing Google User", null);
        var service = CreateAuthService(dbContext, googleUser);

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest("google-id-token"));

        result.IsSuccess.Should().BeTrue();
        (await dbContext.Users.CountAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(1);
        (await dbContext.Users.SingleAsync()).LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GoogleLoginAsync_Should_Return_403_When_Existing_Google_User_Is_Locked()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedGoogleUserAsync(dbContext, "google-uid-1", "existing@example.com", UserStatus.Locked);
        var googleUser = new GoogleUserInfo("google-uid-1", "existing@example.com", true, "Existing Google User", null);
        var service = CreateAuthService(dbContext, googleUser);

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest("google-id-token"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.Error.Should().Be("User account is locked.");
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GoogleLoginAsync_Should_Return_401_When_Existing_Google_User_Is_Deleted()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedGoogleUserAsync(dbContext, "google-uid-1", "existing@example.com", UserStatus.Deleted);
        var googleUser = new GoogleUserInfo("google-uid-1", "existing@example.com", true, "Existing Google User", null);
        var service = CreateAuthService(dbContext, googleUser);

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest("google-id-token"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Error.Should().Be("Invalid Google account.");
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GoogleLoginAsync_Should_Return_409_When_Google_Email_Belongs_To_Another_User()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedGoogleUserAsync(dbContext, "other-google-uid", "conflict@example.com", UserStatus.Active);
        var googleUser = new GoogleUserInfo("new-google-uid", "conflict@example.com", true, "New Google User", null);
        var service = CreateAuthService(dbContext, googleUser);

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest("google-id-token"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        result.Error.Should().Be("Google email already exists.");
        (await dbContext.Users.CountAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GoogleLoginAsync_Should_Return_401_When_IdToken_Is_Invalid()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        var service = CreateAuthService(dbContext, googleUser: null);

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest("invalid"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Error.Should().Be("Invalid Google id token.");
    }

    [Fact]
    public void GoogleLoginRequestValidator_Should_Reject_Empty_IdToken()
    {
        var validator = new GoogleLoginRequestValidator();

        var result = validator.Validate(new GoogleLoginRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GoogleLoginRequest.IdToken));
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static AuthService CreateAuthService(VocaNovaDbContext dbContext, GoogleUserInfo? googleUser)
    {
        return AuthTestFactory.CreateService(dbContext, googleIdentityProvider: new FakeGoogleTokenVerifier(googleUser));
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

    private static async Task SeedUserRoleAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Roles.Add(new Role
        {
            RoleId = 1,
            RoleName = UserRole.User,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedGoogleUserAsync(
        VocaNovaDbContext dbContext,
        string googleUid,
        string googleEmail,
        string status)
    {
        dbContext.Users.Add(new User
        {
            UserId = 100,
            RoleId = 1,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserAuth = new UserAuth
            {
                UserId = 100,
                GoogleUid = googleUid,
                GoogleEmail = googleEmail,
                UpdatedAt = DateTime.UtcNow,
            },
            UserProfile = new EntityUserProfile
            {
                UserId = 100,
                FullName = "Existing Google User",
                UpdatedAt = DateTime.UtcNow,
            },
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        private readonly GoogleUserInfo? _googleUser;

        public FakeGoogleTokenVerifier(GoogleUserInfo? googleUser)
        {
            _googleUser = googleUser;
        }

        public Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_googleUser);
        }
    }
}
