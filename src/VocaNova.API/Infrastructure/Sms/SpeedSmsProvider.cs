using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace VocaNova.API.Infrastructure.Sms;

public sealed class SpeedSmsProvider : ISmsProvider
{
    private readonly HttpClient _httpClient;
    private readonly SpeedSmsSettings _settings;

    public SpeedSmsProvider(
        HttpClient httpClient,
        IOptions<SpeedSmsSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendOtpAsync(
        string phone,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "sms/send");
        request.Headers.Authorization = CreateAuthorizationHeader(_settings.AccessToken);
        request.Content = JsonContent.Create(new SpeedSmsRequest(
            new[] { NormalizeVietnamesePhone(phone) },
            $"Ma OTP VocaNova cua ban la {otpCode}. Ma co hieu luc trong 5 phut.",
            _settings.SmsType,
            _settings.DeviceId));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<SpeedSmsResponse>(
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode
            || result is null
            || !string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase)
            || result.Code != "00")
        {
            throw new HttpRequestException(
                $"SpeedSMS rejected the message (HTTP {(int)response.StatusCode}, "
                + $"code {result?.Code ?? "unknown"}): {result?.Message ?? "No response details."}");
        }
    }

    private static AuthenticationHeaderValue CreateAuthorizationHeader(string accessToken)
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{accessToken}:x"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }

    private static string NormalizeVietnamesePhone(string phone)
    {
        var normalized = new string(phone.Where(character => char.IsDigit(character) || character == '+').ToArray());
        if (normalized.StartsWith("+84", StringComparison.Ordinal))
        {
            return $"0{normalized[3..]}";
        }

        if (normalized.StartsWith("84", StringComparison.Ordinal))
        {
            return $"0{normalized[2..]}";
        }

        return normalized;
    }

    private sealed record SpeedSmsRequest(
        [property: JsonPropertyName("to")] IReadOnlyCollection<string> To,
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("sms_type")] int SmsType,
        [property: JsonPropertyName("sender")] string Sender);

    private sealed record SpeedSmsResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("message")] string? Message);
}
