using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed record LoginRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("password")] string? Password);
