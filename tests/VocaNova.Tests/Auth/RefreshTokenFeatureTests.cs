using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Repositories;
using VocaNova.API.Features.Auth.Services;
using VocaNova.API.Features.Auth.Validators;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Auth;

public class RefreshTokenFeatureTests
{
    private const string RawRefreshToken = "existing-refresh-token";

    [Fact]
    public async Task RefreshTokenAsync_Should_Rotate_Valid_RefreshToken()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserWithRefreshTokenAsync(dbContext, DateTime.UtcNow.AddDays(1), revokedAt: null);
        var service = CreateAuthService(dbContext);

        var result = await service.RefreshTokenAsync(
            new RefreshTokenRequest(RawRefreshToken),
            deviceInfo: "xunit",
            ipAddress: "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBe(RawRefreshToken);
        result.Value.ExpiresIn.Should().Be(900);

        var oldTokenHash = TokenHelper.HashSha256(RawRefreshToken);
        var oldToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == oldTokenHash);
        oldToken.RevokedAt.Should().NotBeNull();

        var newTokenHash = TokenHelper.HashSha256(result.Value.RefreshToken);
        var newToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == newTokenHash);
        newToken.UserId.Should().Be(1);
        newToken.RevokedAt.Should().BeNull();
        newToken.DeviceInfo.Should().Be("xunit");
        newToken.IpAddress.Should().Be("127.0.0.1");

        var principal = CreateJwtTokenService().ValidateAccessToken(result.Value.AccessToken);
        principal.Should().NotBeNull();
        principal!.FindFirst("user_id")!.Value.Should().Be("1");
        principal.FindFirst("role")!.Value.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Return_401_When_RefreshToken_Is_Expired()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserWithRefreshTokenAsync(dbContext, DateTime.UtcNow.AddMinutes(-1), revokedAt: null);
        var service = CreateAuthService(dbContext);

        var result = await service.RefreshTokenAsync(new RefreshTokenRequest(RawRefreshToken));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Error.Should().Be("Refresh token has expired.");
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.SingleAsync()).RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Return_401_When_RefreshToken_Is_Revoked()
    {
        await using var dbContext = CreateDbContext();
        var revokedAt = DateTime.UtcNow.AddMinutes(-5);
        await SeedUserWithRefreshTokenAsync(dbContext, DateTime.UtcNow.AddDays(1), revokedAt);
        var service = CreateAuthService(dbContext);

        var result = await service.RefreshTokenAsync(new RefreshTokenRequest(RawRefreshToken));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Error.Should().Be("Refresh token has been revoked.");
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.SingleAsync()).RevokedAt.Should().Be(revokedAt);
    }

    [Fact]
    public void RefreshTokenRequestValidator_Should_Reject_Empty_RefreshToken()
    {
        var validator = new RefreshTokenRequestValidator();

        var result = validator.Validate(new RefreshTokenRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RefreshTokenRequest.RefreshToken));
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
        return new AuthService(
            dbContext,
            new AuthRepository(dbContext),
            CreateJwtTokenService(),
            new FakeGoogleTokenVerifier(),
            Options.Create(CreateJwtSettings()));
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

    private static async Task SeedUserWithRefreshTokenAsync(
        VocaNovaDbContext dbContext,
        DateTime expiresAt,
        DateTime? revokedAt)
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
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenId = 1,
            UserId = 1,
            TokenHash = TokenHelper.HashSha256(RawRefreshToken),
            DeviceInfo = "old-device",
            IpAddress = "127.0.0.2",
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            RevokedAt = revokedAt,
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        public Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<GoogleUserInfo?>(null);
        }
    }
}
