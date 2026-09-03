using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed record ForgotPasswordRequest(
    [property: JsonPropertyName("phone")] string? Phone);
