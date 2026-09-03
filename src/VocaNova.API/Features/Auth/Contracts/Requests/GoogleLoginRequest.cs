using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed record GoogleLoginRequest(
    [property: JsonPropertyName("id_token")] string? IdToken);
