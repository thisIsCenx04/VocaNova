using System.ComponentModel.DataAnnotations;
using VocaNova.Dashboard.Models.Api.SuperAdmin;

namespace VocaNova.Dashboard.Models.AdminAccounts;

public sealed class AdminAccountListViewModel
{
    public IReadOnlyList<AdminAccount> Items { get; init; } = Array.Empty<AdminAccount>();
    public string? Search { get; init; }
    public string? Status { get; init; }
    public bool IncludeDeleted { get; init; }
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>Cột đang sort (id | name | email | phone | status | created).</summary>
    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public static readonly IReadOnlyList<string> Statuses = ["active", "locked", "deleted"];
}

public sealed class CreateAdminAccountViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    [Display(Name = "Full name")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is invalid.")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression("^(0[3-9]\\d{8})$", ErrorMessage = "Phone must be a valid Vietnamese phone number.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).+$", ErrorMessage = "Password must contain uppercase, lowercase, and digit characters.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "active";
}

public sealed class EditAdminAccountViewModel
{
    public uint AdminId { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    [Display(Name = "Full name")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is invalid.")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression("^(0[3-9]\\d{8})$", ErrorMessage = "Phone must be a valid Vietnamese phone number.")]
    public string? Phone { get; set; }

    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).+$", ErrorMessage = "Password must contain uppercase, lowercase, and digit characters.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
