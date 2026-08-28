using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(uint userId, string role);

    string GenerateRefreshToken();

    AuthPrincipal? ValidateAccessToken(string token);
}
