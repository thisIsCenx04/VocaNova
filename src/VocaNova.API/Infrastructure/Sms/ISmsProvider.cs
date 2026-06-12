namespace VocaNova.API.Infrastructure.Sms;

public interface ISmsProvider
{
    Task SendOtpAsync(string phone, string otpCode, CancellationToken cancellationToken = default);
}
