namespace VocaNova.API.Infrastructure.Sms;

public sealed class ConsoleSmsProvider : ISmsProvider
{
    private readonly ILogger<ConsoleSmsProvider> _logger;

    public ConsoleSmsProvider(ILogger<ConsoleSmsProvider> logger)
    {
        _logger = logger;
    }

    public Task SendOtpAsync(string phone, string otpCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OTP for {Phone}: {OtpCode}", phone, otpCode);
        return Task.CompletedTask;
    }
}
