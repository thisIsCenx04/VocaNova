namespace VocaNova.API.Infrastructure.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; }

    public int RefreshTokenDays { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("JwtSettings:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("JwtSettings:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException("JwtSettings:SecretKey is required.");
        }

        if (AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException("JwtSettings:AccessTokenMinutes must be greater than zero.");
        }

        if (RefreshTokenDays <= 0)
        {
            throw new InvalidOperationException("JwtSettings:RefreshTokenDays must be greater than zero.");
        }
    }
}
