using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed record ChangePasswordRequest(
    [property: JsonPropertyName("current_password")] string? CurrentPassword,
    [property: JsonPropertyName("new_password")] string? NewPassword);
