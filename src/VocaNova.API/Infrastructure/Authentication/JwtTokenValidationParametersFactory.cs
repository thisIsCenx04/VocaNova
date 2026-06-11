using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VocaNova.API.Infrastructure.Authentication;

public static class JwtTokenValidationParametersFactory
{
    public static TokenValidationParameters Create(JwtSettings jwtSettings)
    {
        jwtSettings.Validate();

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
}
