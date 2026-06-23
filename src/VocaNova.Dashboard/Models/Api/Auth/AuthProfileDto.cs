namespace VocaNova.Dashboard.Models.Api.Auth;

// Map từ `GET /api/auth/me` qua ApiJson.Default (SnakeCaseLower). display_name → DisplayName.

public sealed class AuthProfileDto
{
    public uint UserId { get; set; }

    public string? Phone { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
