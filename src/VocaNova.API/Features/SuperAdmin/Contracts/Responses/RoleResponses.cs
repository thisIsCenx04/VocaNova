using System.Text.Json.Serialization;

namespace VocaNova.API.Features.SuperAdmin.Contracts.Responses;

public sealed record RoleResponse(
    [property: JsonPropertyName("role_id")] uint RoleId,
    [property: JsonPropertyName("role_name")] string RoleName);

public sealed record RoleUserResponse(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("status")] string Status);
