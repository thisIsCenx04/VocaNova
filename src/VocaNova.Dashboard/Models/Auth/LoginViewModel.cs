using System.ComponentModel.DataAnnotations;

namespace VocaNova.Dashboard.Models.Auth;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Phone is required.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    public string? ReturnUrl { get; set; }
}
