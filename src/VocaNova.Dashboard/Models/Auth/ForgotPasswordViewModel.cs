using System.ComponentModel.DataAnnotations;

namespace VocaNova.Dashboard.Models.Auth;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression("^(0[3-9]\\d{8})$", ErrorMessage = "Phone must be a valid Vietnamese phone number.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }
}
