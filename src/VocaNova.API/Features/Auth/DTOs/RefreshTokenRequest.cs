using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record RefreshTokenRequest(
    [property: JsonPropertyName("refresh_token")] string? RefreshToken);
