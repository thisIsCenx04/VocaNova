using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.BLL.Abstractions;

namespace VocaNova.API.Infrastructure.Authentication;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken) => TokenHelper.HashSha256(refreshToken);
}
