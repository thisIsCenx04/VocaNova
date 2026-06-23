using VocaNova.Dashboard.Models.Api.Auth;

namespace VocaNova.Dashboard.Models.Profile;

public sealed class ProfileViewModel
{
    public AuthProfileDto? Profile { get; init; }

    public bool Loaded { get; init; }

    /// <summary>Giá trị form sửa tên (giữ lại khi validate fail).</summary>
    public string? DisplayName { get; set; }
}

public sealed class SettingsViewModel
{
    /// <summary>"light" | "dark" — đọc từ cookie VocaNova.Dashboard.Theme.</summary>
    public string Theme { get; init; } = "light";

    /// <summary>"en" | "vi" — culture hiện tại.</summary>
    public string Culture { get; init; } = "en";
}
