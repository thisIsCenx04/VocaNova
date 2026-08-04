using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Security;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.Tests.Shared;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_Should_Create_Valid_Token_With_UserId_Role_And_Jti()
    {
        var service = CreateService();

        var token = service.GenerateAccessToken(userId: 123, role: UserRole.Admin);
        var principal = service.ValidateAccessToken(token);

        principal.Should().NotBeNull();
        if (principal is null)
        {
            throw new InvalidOperationException("Expected a validated principal.");
        }

        principal.FindFirstValue("sub").Should().Be("123");
        principal.FindFirstValue("user_id").Should().Be("123");
        principal.FindFirstValue("role").Should().Be(UserRole.Admin);
        principal.FindFirstValue(JwtRegisteredClaimNames.Jti).Should().NotBeNullOrWhiteSpace();
        principal.IsInRole(UserRole.Admin).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_Should_Set_Expected_Issuer_Audience_And_Expiry()
    {
        var service = CreateService();
        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.ReadJwtToken(service.GenerateAccessToken(123, UserRole.User));

        token.Issuer.Should().Be("VocaNova.Tests");
        token.Audiences.Should().ContainSingle("VocaNova.Tests.Clients");
        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_UuidV4_Raw_String()
    {
        var service = CreateService();

        var refreshToken = service.GenerateRefreshToken();

        Guid.TryParse(refreshToken, out var parsedToken).Should().BeTrue();
        refreshToken.Should().HaveLength(36);
        refreshToken[14].Should().Be('4');
        parsedToken.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void RefreshToken_Should_Be_Hashable_For_Database_Storage()
    {
        var service = CreateService();
        var refreshToken = service.GenerateRefreshToken();

        var tokenHash = TokenHelper.HashSha256(refreshToken);

        tokenHash.Should().HaveLength(64);
        tokenHash.Should().MatchRegex("^[0-9a-f]{64}$");
        tokenHash.Should().NotBe(refreshToken);
    }

    [Fact]
    public void ValidateAccessToken_Should_Return_Null_For_Invalid_Token()
    {
        var service = CreateService();

        var principal = service.ValidateAccessToken("not-a-jwt-token");

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_Should_Return_Null_For_Expired_Token()
    {
        var service = CreateService();
        var expiredToken = CreateExpiredToken();

        var principal = service.ValidateAccessToken(expiredToken);

        principal.Should().BeNull();
    }

    private const string TestSecretKey = "THIS_IS_A_TEST_SECRET_KEY_WITH_32_CHARS_MIN";

    private static JwtTokenService CreateService()
    {
        return new JwtTokenService(Options.Create(CreateSettings()));
    }

    private static JwtSettings CreateSettings()
    {
        return new JwtSettings
        {
            Issuer = "VocaNova.Tests",
            Audience = "VocaNova.Tests.Clients",
            SecretKey = TestSecretKey,
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        };
    }

    private static string CreateExpiredToken()
    {
        var now = DateTime.UtcNow;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var token = new JwtSecurityToken(
            issuer: "VocaNova.Tests",
            audience: "VocaNova.Tests.Clients",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "123"),
                new Claim("role", UserRole.User),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            },
            notBefore: now.AddMinutes(-30),
            expires: now.AddMinutes(-15),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
