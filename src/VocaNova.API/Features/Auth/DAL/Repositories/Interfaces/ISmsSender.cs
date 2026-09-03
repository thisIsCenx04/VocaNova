namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface ISmsSender
{
    Task SendOtpAsync(string phone, string code, CancellationToken cancellationToken = default);
}
