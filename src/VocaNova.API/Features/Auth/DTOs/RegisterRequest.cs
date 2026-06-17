using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record RegisterRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("password")] string? Password,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("otp_code")] string? OtpCode);
