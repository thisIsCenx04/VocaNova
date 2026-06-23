using System.ComponentModel.DataAnnotations;

namespace VocaNova.Dashboard.Models.Auth;

public sealed class ForgotPasswordViewModel
{
    /// <summary>"request" = đang nhập số điện thoại; "reset" = đã gửi OTP, nhập mã + mật khẩu mới.</summary>
    public string Step { get; set; } = "request";

    [Required]
    [RegularExpression(@"^0[3-9]\d{8}$", ErrorMessage = "Phone is not a valid Vietnamese number.")]
    public string? Phone { get; set; }

    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits.")]
    public string? OtpCode { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }

    /// <summary>Số giây OTP còn hiệu lực (hiển thị đếm ngược ở bước reset).</summary>
    public int ExpiresIn { get; set; }

    /// <summary>Thông báo thành công (vd: đã gửi OTP).</summary>
    public string? Info { get; set; }
}
