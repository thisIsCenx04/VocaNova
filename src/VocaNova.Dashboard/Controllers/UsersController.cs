using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using VocaNova.Dashboard.Models.Api.Users;
using VocaNova.Dashboard.Models.Users;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class UsersController : Controller
{
    private static readonly IReadOnlySet<string> ValidStatuses =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "active", "locked", "deleted" };

    private readonly IVocaNovaApiClient _api;
    private readonly IStringLocalizer<SharedResource> _l;

    public UsersController(IVocaNovaApiClient api, IStringLocalizer<SharedResource> l)
    {
        _api = api;
        _l = l;
    }

    [HttpGet("/users")]
    public async Task<IActionResult> Index([FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            query.Page = 1;
        }

        // Status không hợp lệ → bỏ qua để API không trả lỗi (giữ trang sống).
        if (!string.IsNullOrWhiteSpace(query.Status) && !ValidStatuses.Contains(query.Status))
        {
            query.Status = null;
        }

        var users = await _api.GetPagedAsync<AdminUserSummaryDto>(BuildUsersUrl(query), cancellationToken);

        return View(new UserListViewModel
        {
            Query = query,
            Users = users.IsSuccess ? users.Items : Array.Empty<AdminUserSummaryDto>(),
            Loaded = users.IsSuccess,
            Pagination = users.Pagination,
        });
    }

    [HttpGet("/users/{id:long}")]
    public async Task<IActionResult> Detail(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.GetAsync<AdminUserDetailDto>($"api/admin/users/{id}", cancellationToken);

        return View(new UserDetailViewModel
        {
            User = result.IsSuccess ? result.Data : null,
            Loaded = result.IsSuccess && result.Data is not null,
            ErrorMessage = result.Message,
            // G4/G8/G5: chưa có API → nút disabled / tab Empty, tự sáng đèn khi An deploy.
            LockUnlockAvailable = false,
            EditAvailable = false,
            HistoryAvailable = false,
        });
    }

    [HttpPost("/users/{id:long}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(uint id, CancellationToken cancellationToken)
    {
        // API yêu cầu super_admin policy; admin thường sẽ nhận 403 → message hiện qua toast.
        var result = await _api.PatchAsync<object>($"api/admin/users/{id}/deactivate", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Toast.UserDeactivated", result.Message);
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/users/{id:long}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.PatchAsync<object>($"api/admin/users/{id}/restore", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Toast.UserRestored", result.Message);
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Khung sẵn theo contract API còn thiếu — UI giữ nút disabled tới khi An deploy:
    //  G4: PATCH /api/admin/users/{id}/lock + /unlock
    //  G8: POST /api/admin/users + PUT /api/admin/users/{id}
    [HttpPost("/users/{id:long}/lock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.PatchAsync<object>($"api/admin/users/{id}/lock", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Toast.UserLocked", result.Message);
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/users/{id:long}/unlock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.PatchAsync<object>($"api/admin/users/{id}/unlock", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Toast.UserUnlocked", result.Message);
        return RedirectToAction(nameof(Detail), new { id });
    }

    private static string BuildUsersUrl(UserListQuery q)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = q.Page.ToString(),
            ["limit"] = q.Limit.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            parameters["search"] = q.Search;
        }
        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            parameters["status"] = q.Status;
        }

        return QueryHelpers.AddQueryString("api/admin/users", parameters);
    }

    private void SetToast(bool success, string successKey, string? errorMessage)
    {
        TempData["ToastKind"] = success ? "ok" : "err";
        TempData["ToastMessage"] = success
            ? _l[successKey].Value
            : (string.IsNullOrWhiteSpace(errorMessage) ? _l["Toast.ActionFailed"].Value : errorMessage);
    }
}
