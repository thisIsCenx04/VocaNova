namespace VocaNova.Dashboard.Models.Api.AdminAccounts;

// Contract G9 (An làm) — dashboard dựng khung theo shape này, map SnakeCaseLower.
//   GET    /api/admin/admin-accounts?q=&page=&limit=  → PagedResult<AdminAccountDto>
//   POST   /api/admin/admin-accounts                  { phone, display_name, password, role } → AdminAccountDto
//   PUT    /api/admin/admin-accounts/{id}             { display_name, role }                  → AdminAccountDto
//   DELETE /api/admin/admin-accounts/{id}             (soft delete, super_admin)
public sealed class AdminAccountDto
{
    public uint UserId { get; set; }

    public string? Phone { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
