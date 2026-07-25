using System.Text.Json.Serialization;

namespace VocaNova.API.Features.SuperAdmin.DTOs;

public sealed record SaveAdminUserAssignmentsRequest(
    [property: JsonPropertyName("user_ids")] IReadOnlyCollection<uint>? UserIds);

public sealed record AssignmentAdminDto(
    [property: JsonPropertyName("admin_id")] uint AdminId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("email")] string? Email);

public sealed record AssignmentUserDto(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("assigned_admin_id")] uint? AssignedAdminId);

public sealed record AdminUserAssignmentOverviewDto(
    [property: JsonPropertyName("admins")] IReadOnlyCollection<AssignmentAdminDto> Admins,
    [property: JsonPropertyName("users")] IReadOnlyCollection<AssignmentUserDto> Users);
