using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IRefreshTokenRepository
{
    Task StageCreateAsync(CreateRefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshTokenRecord?> FindByHashAsync(string hash, CancellationToken cancellationToken = default);

    Task<RefreshTokenRecord?> FindForUpdateByHashAsync(string hash, CancellationToken cancellationToken = default);

    Task<bool> StageRevokeAsync(string hash, DateTime revokedAt, CancellationToken cancellationToken = default);

    Task<int> StageRevokeAllAsync(uint userId, DateTime revokedAt, CancellationToken cancellationToken = default);
}
