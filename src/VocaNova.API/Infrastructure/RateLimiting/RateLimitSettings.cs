namespace VocaNova.API.Infrastructure.RateLimiting;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimit";

    public int OtpPerMinutePerPhone { get; set; } = 1;

    public int OtpPerMinutePerIp { get; set; } = 1;

    public int LoginPerMinutePerIp { get; set; } = 10;

    public int RetryAfterSeconds { get; set; } = 60;
}
