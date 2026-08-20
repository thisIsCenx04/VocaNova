using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed record OtpVerifyRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("otp_code")] string? OtpCode);
