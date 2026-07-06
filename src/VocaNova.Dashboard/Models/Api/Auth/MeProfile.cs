namespace VocaNova.Dashboard.Models.Api.Auth;

/// <summary>Hồ sơ của admin đang đăng nhập (GET /api/auth/me). Bỏ qua learning_profile (không dùng ở dashboard).</summary>
public sealed class MeProfile
{
    public uint UserId { get; set; }

    public string? Phone { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
