using VocaNova.API.Common.Results;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Services;

public interface IAuthService
{
    Task<Result<TokenResponse>> RegisterAsync(
        RegisterRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
