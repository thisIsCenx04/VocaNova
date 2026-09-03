using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IGoogleIdentityProvider
{
    Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}
