namespace VocaNova.API.Infrastructure.RateLimiting;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimit";

    private int _otpPerMinutePerPhone = 1;
    private int _otpPerMinutePerIp = 1;
    private int _loginPerMinutePerIp = 10;
    private int _retryAfterSeconds = 60;

    public int OtpPerMinutePerPhone
    {
        get => _otpPerMinutePerPhone;
        set => _otpPerMinutePerPhone = UseConfiguredOrDefault(value, _otpPerMinutePerPhone);
    }

    public int OtpPerMinutePerIp
    {
        get => _otpPerMinutePerIp;
        set => _otpPerMinutePerIp = UseConfiguredOrDefault(value, _otpPerMinutePerIp);
    }

    public int LoginPerMinutePerIp
    {
        get => _loginPerMinutePerIp;
        set => _loginPerMinutePerIp = UseConfiguredOrDefault(value, _loginPerMinutePerIp);
    }

    public int RetryAfterSeconds
    {
        get => _retryAfterSeconds;
        set => _retryAfterSeconds = UseConfiguredOrDefault(value, _retryAfterSeconds);
    }

    private static int UseConfiguredOrDefault(int configuredValue, int defaultValue)
    {
        return configuredValue > 0 ? configuredValue : defaultValue;
    }
}
