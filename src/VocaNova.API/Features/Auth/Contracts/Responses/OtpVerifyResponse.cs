using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Responses;

public sealed record OtpVerifyResponse(
    [property: JsonPropertyName("verified")] bool Verified);
