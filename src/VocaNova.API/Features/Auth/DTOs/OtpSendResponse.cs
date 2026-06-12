using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record OtpSendResponse(
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
