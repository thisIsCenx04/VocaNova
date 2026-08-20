using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Responses;

public sealed record OtpSendResponse(
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
