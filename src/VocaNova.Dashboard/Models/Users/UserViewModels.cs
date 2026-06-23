using VocaNova.Dashboard.Models.Api.Users;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Models.Users;

/// <summary>Tham số filter/paging bind từ query string của trang user management.</summary>
public sealed class UserListQuery
{
    /// <summary>Tìm theo số điện thoại / tên hiển thị.</summary>
    public string? Search { get; set; }

    /// <summary>active | locked | deleted (khớp UserStatus bên API).</summary>
    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int Limit { get; set; } = 20;
}

public sealed class UserListViewModel
{
    public UserListQuery Query { get; init; } = new();

    public IReadOnlyList<AdminUserSummaryDto> Users { get; init; } = Array.Empty<AdminUserSummaryDto>();

    public bool Loaded { get; init; }

    public PaginationInfo? Pagination { get; init; }

    /// <summary>Các giá trị status hợp lệ để dựng dropdown filter.</summary>
    public static readonly IReadOnlyList<string> StatusOptions = new[] { "active", "locked", "deleted" };
}

public sealed class UserDetailViewModel
{
    public AdminUserDetailDto? User { get; init; }

    public bool Loaded { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>G4: lock/unlock chưa có API → nút disabled "Sắp có".</summary>
    public bool LockUnlockAvailable { get; init; }

    /// <summary>G8: create/update user chưa có API → nút disabled "Sắp có".</summary>
    public bool EditAvailable { get; init; }

    /// <summary>G5: test-history + activity cho admin chưa có API → tab Empty.</summary>
    public bool HistoryAvailable { get; init; }
}
