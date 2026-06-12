using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record ResetPasswordRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("otp_code")] string? OtpCode,
    [property: JsonPropertyName("new_password")] string? NewPassword);
