using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VocaNova.API.Common.Constants;

namespace VocaNova.API.Infrastructure.Authentication;

public static class JwtAuthenticationExtensions
{
    public const string AdminPolicy = "Admin";
    public const string SuperAdminPolicy = "SuperAdmin";
    public const string UserPolicy = "User";

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is required.");

        jwtSettings.Validate();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = CreateTokenValidationParameters(jwtSettings);
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        AddUserIdClaimFromSubject(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }

    public static IServiceCollection AddVocaNovaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy =>
                policy.RequireRole(UserRole.Admin, UserRole.SuperAdmin));

            options.AddPolicy(SuperAdminPolicy, policy =>
                policy.RequireRole(UserRole.SuperAdmin));

            options.AddPolicy(UserPolicy, policy =>
                policy.RequireRole(UserRole.User, UserRole.Admin, UserRole.SuperAdmin));
        });

        return services;
    }

    public static IServiceCollection AddSwaggerWithJwtBearer(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter a JWT bearer token.",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme,
                        },
                    }
                ] = Array.Empty<string>(),
            });
        });

        return services;
    }

    private static TokenValidationParameters CreateTokenValidationParameters(JwtSettings jwtSettings)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "user_id",
            RoleClaimType = "role",
        };
    }

    private static void AddUserIdClaimFromSubject(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        if (identity.HasClaim(claim => claim.Type == "user_id"))
        {
            return;
        }

        var subject = identity.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            identity.AddClaim(new Claim("user_id", subject));
        }
    }
}
