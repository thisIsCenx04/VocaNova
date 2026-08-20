using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Auth.BLL.Abstractions;

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
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IGoogleIdentityProvider, GoogleTokenVerifier>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = JwtTokenValidationParametersFactory.Create(jwtSettings);
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        JwtClaimsPrincipalHelper.AddUserIdClaimFromSubject(context.Principal);
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
}
