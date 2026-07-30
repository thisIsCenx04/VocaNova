using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.AdminAccounts;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize(Roles = "super_admin")]
public sealed class AdminAccountsController : Controller
{
    private const int PageSize = 10;
    private readonly IVocaNovaApiClient _apiClient;

    public AdminAccountsController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/admin-accounts")]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? sortBy = null,
        string? sortDirection = null,
        bool includeDeleted = false,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
        sortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim();
        sortDirection = string.IsNullOrWhiteSpace(sortDirection) ? null : sortDirection.Trim();
        var accounts = await _apiClient.GetAdminAccountsAsync(
            // Danh sách phân trang phía server nên sort do API xử lý.
            new AdminAccountFilter(status, search, includeDeleted, page, PageSize, sortBy, sortDirection),
            cancellationToken);

        return View(new AdminAccountListViewModel
        {
            Items = accounts.Items,
            Search = search,
            Status = status,
            IncludeDeleted = includeDeleted,
            Page = accounts.Page,
            Limit = accounts.Limit,
            TotalItems = accounts.TotalItems,
            TotalPages = accounts.TotalPages,
            SortBy = sortBy,
            SortDirection = sortDirection,
        });
    }

    [HttpGet("/admin-accounts/create")]
    public IActionResult Create() => View(new CreateAdminAccountViewModel());

    [HttpPost("/admin-accounts/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateAdminAccountViewModel model,
        CancellationToken cancellationToken)
    {
        ValidateStatus(model.Status);
        if (!ModelState.IsValid) return View(model);

        var result = await _apiClient.CreateAdminAccountAsync(new AdminAccountInput(
            model.FullName?.Trim(), model.Email?.Trim(), model.Phone?.Trim(), model.Password, model.Status),
            cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, ErrorMessage(result, "Could not create the admin account."));
            return View(model);
        }

        TempData["AdminAccountSuccess"] = "Admin account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/admin-accounts/{id:uint}/edit")]
    public async Task<IActionResult> Edit(uint id, CancellationToken cancellationToken)
    {
        var account = await _apiClient.GetAdminAccountAsync(id, cancellationToken);
        if (account is null || account.Status == "deleted") return NotFound();

        return View(new EditAdminAccountViewModel
        {
            AdminId = account.AdminId,
            FullName = account.FullName,
            Email = account.Email,
            Phone = account.Phone,
            Status = account.Status,
            CreatedAt = account.CreatedAt,
            LastLoginAt = account.LastLoginAt,
        });
    }

    [HttpPost("/admin-accounts/{id:uint}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        uint id,
        EditAdminAccountViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.AdminId != id) return BadRequest();
        ValidateStatus(model.Status);
        if (!ModelState.IsValid) return View(model);

        var result = await _apiClient.UpdateAdminAccountAsync(id, new AdminAccountInput(
            model.FullName?.Trim(), model.Email?.Trim(), model.Phone?.Trim(),
            string.IsNullOrWhiteSpace(model.Password) ? null : model.Password,
            model.Status), cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, ErrorMessage(result, "Could not update the admin account."));
            return View(model);
        }

        TempData["AdminAccountSuccess"] = "Admin account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin-accounts/{id:uint}/lock")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Lock(uint id, string? returnUrl, CancellationToken cancellationToken) =>
        RunAction(() => _apiClient.LockAdminAccountAsync(id, cancellationToken), "Admin account locked.", returnUrl);

    [HttpPost("/admin-accounts/{id:uint}/unlock")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unlock(uint id, string? returnUrl, CancellationToken cancellationToken) =>
        RunAction(() => _apiClient.UnlockAdminAccountAsync(id, cancellationToken), "Admin account unlocked.", returnUrl);

    [HttpPost("/admin-accounts/{id:uint}/delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(uint id, string? returnUrl, CancellationToken cancellationToken) =>
        RunAction(() => _apiClient.DeleteAdminAccountAsync(id, cancellationToken), "Admin account deleted.", returnUrl);

    private async Task<IActionResult> RunAction(
        Func<Task<ApiActionResult>> action,
        string successMessage,
        string? returnUrl)
    {
        var result = await action();
        TempData[result.IsSuccess ? "AdminAccountSuccess" : "AdminAccountError"] =
            result.IsSuccess ? successMessage : ErrorMessage(result, "The request could not be completed.");
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private void ValidateStatus(string? status)
    {
        if (status is not ("active" or "locked"))
        {
            ModelState.AddModelError(nameof(status), "Status must be active or locked.");
        }
    }

    private static string ErrorMessage(ApiActionResult result, string fallback) => result.StatusCode switch
    {
        401 or 403 => "Only Super Admin can perform this action.",
        404 => "Admin account not found.",
        409 => result.Message ?? "Email, phone, or account state conflicts with existing data.",
        _ => result.Message ?? fallback,
    };
}
