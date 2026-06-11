using System.Security.Claims;

namespace VocaNova.API.Infrastructure.Authentication;

public interface IJwtTokenService
{
    string GenerateAccessToken(uint userId, string role);

    string GenerateRefreshToken();

    ClaimsPrincipal? ValidateAccessToken(string token);
}
