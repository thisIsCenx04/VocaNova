using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record OtpVerifyResponse(
    [property: JsonPropertyName("verified")] bool Verified);
