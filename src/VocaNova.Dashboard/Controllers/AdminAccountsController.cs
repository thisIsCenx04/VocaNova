using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.AdminAccounts;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize(Roles = "super_admin")]
public sealed class AdminAccountsController : Controller
{
    private const int PageSize = 10;
    private const int RolePageSize = 30;
    private readonly IVocaNovaApiClient _apiClient;

    public AdminAccountsController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/admin-accounts/role-assignment")]
    public async Task<IActionResult> RoleAssignment(
        string? search,
        string role = "user",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        role = role is "admin" ? "admin" : "user";
        page = Math.Max(1, page);
        var accounts = await _apiClient.GetUsersAsync(
            new UserListFilter(null, search, false, page, RolePageSize, role),
            cancellationToken);

        return View(new AccountRoleAssignmentViewModel
        {
            Items = accounts.Items,
            Search = search,
            Role = role,
            Page = accounts.Page,
            Limit = accounts.Limit,
            TotalItems = accounts.TotalItems,
            TotalPages = accounts.TotalPages,
        });
    }

    [HttpPost("/admin-accounts/{id:uint}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(
        uint id,
        string role,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        role = role?.Trim().ToLowerInvariant() ?? string.Empty;
        if (role is not ("user" or "admin"))
        {
            TempData["AdminAccountError"] = "Only user and admin roles can be assigned here.";
            return SafeRedirect(returnUrl);
        }

        var roles = await _apiClient.GetRolesAsync(cancellationToken);
        var targetRole = roles.Items.FirstOrDefault(item => item.RoleName == role);
        if (targetRole is null)
        {
            TempData["AdminAccountError"] = "Target role was not found.";
            return SafeRedirect(returnUrl);
        }

        var result = await _apiClient.AssignRoleAsync(targetRole.RoleId, id, cancellationToken);
        TempData[result.IsSuccess ? "AdminAccountSuccess" : "AdminAccountError"] =
            result.IsSuccess
                ? role == "admin" ? "User promoted to administrator." : "Administrator changed to user."
                : ErrorMessage(result, "Could not change the account role.");
        return SafeRedirect(returnUrl);
    }

    [HttpGet("/admin-accounts")]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        bool includeDeleted = false,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
        var accounts = await _apiClient.GetAdminAccountsAsync(
            new AdminAccountFilter(status, search, includeDeleted, page, PageSize),
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

    private IActionResult SafeRedirect(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(RoleAssignment));

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
