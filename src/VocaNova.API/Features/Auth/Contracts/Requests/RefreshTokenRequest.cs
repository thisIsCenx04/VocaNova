using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed record RefreshTokenRequest(
    [property: JsonPropertyName("refresh_token")] string? RefreshToken);
