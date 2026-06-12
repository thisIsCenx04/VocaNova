using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record ForgotPasswordRequest(
    [property: JsonPropertyName("phone")] string? Phone);
