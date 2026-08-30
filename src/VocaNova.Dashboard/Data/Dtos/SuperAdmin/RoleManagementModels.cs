using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Data.Dtos.SuperAdmin;

public sealed record ManagedRole(
    [property: JsonPropertyName("role_id")] uint RoleId,
    [property: JsonPropertyName("role_name")] string RoleName);
