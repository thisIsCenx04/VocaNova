using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record GoogleLoginRequest(
    [property: JsonPropertyName("id_token")] string? IdToken);
