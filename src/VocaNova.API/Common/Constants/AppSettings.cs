namespace VocaNova.API.Common.Constants;

public static class AppSettings
{
    public const int MaxListsPerUser = 50;
    public const double AiPassThreshold = 0.75;

    public const int DefaultPage = 1;
    public const int DefaultPageLimit = 20;
    public const int MaxPageLimit = 100;

    public const int OtpCodeLength = 6;
    public const int OtpTtlMinutes = 5;
    public const int OtpMaxVerifyAttempts = 5;

    public const int AccessTokenMinutes = 15;
    public const int RefreshTokenDays = 30;

    public const int MinRegistrationAge = 5;
    public const int MaxRegistrationAge = 120;
}
