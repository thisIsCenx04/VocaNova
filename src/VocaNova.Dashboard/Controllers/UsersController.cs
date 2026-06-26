using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Users;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F060 — User Management. List/filter + detail tabs (Profile/Learning/Test History/Activity).
// Deactivate/Restore yêu cầu super_admin (API enforce SuperAdminPolicy).
public sealed class UsersController : Controller
{
    private const int PageSize = 10;
    private const int TabPageSize = 10;

    private readonly IVocaNovaApiClient _apiClient;

    public UsersController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/users")]
    public async Task<IActionResult> Index(
        string? status,
        string? search,
        string? role,
        bool includeDeleted = false,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        var filter = new UserListFilter(
            Status: string.IsNullOrWhiteSpace(status) ? null : status,
            Search: string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            IncludeDeleted: includeDeleted,
            Page: page,
            Limit: PageSize,
            Role: string.IsNullOrWhiteSpace(role) ? null : role);

        var users = await _apiClient.GetUsersAsync(filter, cancellationToken);

        var model = new UserListViewModel
        {
            Items = users.Items,
            Status = filter.Status,
            Search = filter.Search,
            Role = filter.Role,
            IncludeDeleted = includeDeleted,
            Page = users.Page,
            Limit = users.Limit,
            TotalItems = users.TotalItems,
            TotalPages = users.TotalPages,
        };

        return View(model);
    }

    [HttpGet("/users/{id:uint}")]
    public async Task<IActionResult> Detail(uint id, CancellationToken cancellationToken)
    {
        var detail = await _apiClient.GetUserDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        var topics = await _apiClient.GetUserTopicsAsync(id, cancellationToken);
        var testHistory = await _apiClient.GetUserTestHistoryAsync(id, 1, TabPageSize, cancellationToken);
        var auditLogs = await _apiClient.GetUserAuditLogsAsync(id, 1, TabPageSize, cancellationToken);

        var model = new UserDetailViewModel
        {
            Detail = detail,
            Topics = topics,
            TestHistory = testHistory,
            AuditLogs = auditLogs,
        };

        return View(model);
    }

    [HttpPost("/users/{id:uint}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.DeactivateUserAsync(id, cancellationToken);
        SetFeedback(result, "User deactivated.");
        return RedirectToSafe(returnUrl);
    }

    [HttpPost("/users/{id:uint}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.RestoreUserAsync(id, cancellationToken);
        SetFeedback(result, "User restored.");
        return RedirectToSafe(returnUrl);
    }

    // Ở lại trang vừa thao tác (danh sách hoặc chi tiết) thay vì luôn nhảy sang detail.
    private IActionResult RedirectToSafe(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));

    private void SetFeedback(ApiActionResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            TempData["UserSuccess"] = successMessage;
            return;
        }

        TempData["UserError"] = result.StatusCode switch
        {
            401 or 403 => "You do not have permission (super admin required).",
            404 => "User not found.",
            409 => result.Message ?? "User is not in a state that allows this action.",
            _ => result.Message ?? "The request could not be completed.",
        };
    }
}
