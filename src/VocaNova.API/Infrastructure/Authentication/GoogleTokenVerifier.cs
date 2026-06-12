using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace VocaNova.API.Infrastructure.Authentication;

public sealed class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly GoogleAuthSettings _settings;

    public GoogleTokenVerifier(IOptions<GoogleAuthSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<GoogleUserInfo?> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = _settings.ClientIds.Length == 0 ? null : _settings.ClientIds,
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            if (string.IsNullOrWhiteSpace(payload.Subject))
            {
                return null;
            }

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
