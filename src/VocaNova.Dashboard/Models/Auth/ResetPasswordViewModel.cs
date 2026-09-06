using System.ComponentModel.DataAnnotations;

namespace VocaNova.Dashboard.Models.Auth;

public sealed class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression("^(0[3-9]\\d{8})$", ErrorMessage = "Phone must be a valid Vietnamese phone number.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "OTP code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits.")]
    [RegularExpression("^[0-9]+$", ErrorMessage = "OTP code must contain digits only.")]
    [Display(Name = "OTP code")]
    public string? OtpCode { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).+$", ErrorMessage = "Password must contain uppercase, lowercase, and digit characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password must match the new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string? ConfirmPassword { get; set; }
}
