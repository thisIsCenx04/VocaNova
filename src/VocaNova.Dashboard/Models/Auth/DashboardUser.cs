namespace VocaNova.Dashboard.Models.Auth;

public sealed record DashboardUser(
    uint UserId,
    string? Phone,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    string Status);
