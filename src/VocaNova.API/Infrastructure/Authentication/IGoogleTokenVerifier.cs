namespace VocaNova.API.Infrastructure.Authentication;

public interface IGoogleTokenVerifier
{
    Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}
