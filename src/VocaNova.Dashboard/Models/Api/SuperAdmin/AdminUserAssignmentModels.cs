using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.SuperAdmin;

public sealed record AssignmentAdmin(
    [property: JsonPropertyName("admin_id")] uint AdminId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("email")] string? Email);

public sealed record AssignmentUser(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("assigned_admin_id")] uint? AssignedAdminId);

public sealed record AdminUserAssignmentOverview(
    [property: JsonPropertyName("admins")] IReadOnlyList<AssignmentAdmin> Admins,
    [property: JsonPropertyName("users")] IReadOnlyList<AssignmentUser> Users);
