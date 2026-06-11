using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VocaNova.API.Common.Constants;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.Tests.Shared;

public class JwtAuthenticationTests
{
    [Fact]
    public void AddJwtAuthentication_Should_Configure_Bearer_Defaults_And_Token_Validation()
    {
        using var provider = BuildServiceProvider();

        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var jwtOptions = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        authOptions.DefaultAuthenticateScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        authOptions.DefaultChallengeScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        provider.GetService<IJwtTokenService>().Should().BeOfType<JwtTokenService>();

        jwtOptions.MapInboundClaims.Should().BeFalse();
        jwtOptions.TokenValidationParameters.ValidIssuer.Should().Be("VocaNova.Tests");
        jwtOptions.TokenValidationParameters.ValidAudience.Should().Be("VocaNova.Tests.Clients");
        jwtOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        jwtOptions.TokenValidationParameters.RoleClaimType.Should().Be("role");
        jwtOptions.TokenValidationParameters.IssuerSigningKey
            .Should()
            .BeOfType<SymmetricSecurityKey>()
            .Which.Key
            .Should()
            .Equal(Encoding.UTF8.GetBytes(TestSecretKey));
    }

    [Fact]
    public void AddVocaNovaAuthorization_Should_Register_Role_Based_Policies()
    {
        using var provider = BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        GetAllowedRoles(options, JwtAuthenticationExtensions.AdminPolicy)
            .Should()
            .BeEquivalentTo(new[] { UserRole.Admin, UserRole.SuperAdmin });

        GetAllowedRoles(options, JwtAuthenticationExtensions.SuperAdminPolicy)
            .Should()
            .BeEquivalentTo(new[] { UserRole.SuperAdmin });

        GetAllowedRoles(options, JwtAuthenticationExtensions.UserPolicy)
            .Should()
            .BeEquivalentTo(new[] { UserRole.User, UserRole.Admin, UserRole.SuperAdmin });
    }

    [Fact]
    public async Task JwtBearerEvents_Should_Add_UserId_Claim_From_Subject()
    {
        using var provider = BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "123"),
                new Claim("role", UserRole.Admin),
            },
            JwtBearerDefaults.AuthenticationScheme,
            nameType: "user_id",
            roleType: "role");
        var principal = new ClaimsPrincipal(identity);

        var context = new TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            options)
        {
            Principal = principal,
        };

        await options.Events.TokenValidated(context);

        principal.FindFirstValue("user_id").Should().Be("123");
        principal.IsInRole(UserRole.Admin).Should().BeTrue();
    }

    private const string TestSecretKey = "THIS_IS_A_TEST_SECRET_KEY_WITH_32_CHARS_MIN";

    private static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "VocaNova.Tests",
                ["JwtSettings:Audience"] = "VocaNova.Tests.Clients",
                ["JwtSettings:SecretKey"] = TestSecretKey,
                ["JwtSettings:AccessTokenMinutes"] = "15",
                ["JwtSettings:RefreshTokenDays"] = "30",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddJwtAuthentication(configuration);
        services.AddVocaNovaAuthorization();

        return services.BuildServiceProvider();
    }

    private static IReadOnlyCollection<string> GetAllowedRoles(AuthorizationOptions options, string policyName)
    {
        var policy = options.GetPolicy(policyName);

        policy.Should().NotBeNull();

        return policy!.Requirements
            .OfType<RolesAuthorizationRequirement>()
            .Single()
            .AllowedRoles
            .ToArray();
    }
}
