using System.Text.Json.Serialization;

namespace VocaNova.API.Features.SuperAdmin.Contracts.Responses;

public sealed record AdminAccountResponse(
    [property: JsonPropertyName("admin_id")] uint AdminId,
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
    [property: JsonPropertyName("last_login_at")] DateTime? LastLoginAt);
