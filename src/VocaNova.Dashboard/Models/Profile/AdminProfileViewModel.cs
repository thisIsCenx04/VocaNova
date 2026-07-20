namespace VocaNova.Dashboard.Models.Profile;

/// <summary>F063A — dữ liệu trang hồ sơ của admin đang đăng nhập.</summary>
public sealed class AdminProfileViewModel
{
    public uint UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    // Số điện thoại chỉ để xem (masked), không cho sửa ở màn này.
    public string MaskedPhone => Mask(Phone);

    public string Initial => string.IsNullOrWhiteSpace(DisplayName)
        ? "A"
        : DisplayName.Trim()[..1].ToUpperInvariant();

    private static string Mask(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return "—";
        }

        var p = phone.Trim();
        if (p.Length <= 4)
        {
            return p;
        }

        return p[..3] + new string('*', p.Length - 5) + p[^2..];
    }
}
