using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.SuperAdmin;

public sealed record ManagedRole(
    [property: JsonPropertyName("role_id")] uint RoleId,
    [property: JsonPropertyName("role_name")] string RoleName);
