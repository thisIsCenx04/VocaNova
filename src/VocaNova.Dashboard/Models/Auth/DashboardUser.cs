namespace VocaNova.Dashboard.Models.Auth;

/// <summary>Thông tin tài khoản admin sau khi đăng nhập, lưu vào claims của cookie.</summary>
public sealed record DashboardUser(
    uint UserId,
    string? Phone,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    string Status);
