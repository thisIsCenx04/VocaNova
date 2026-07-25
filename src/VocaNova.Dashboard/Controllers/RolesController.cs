using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Roles;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize(Roles = "super_admin")]
public sealed class RolesController : Controller
{
    private readonly IVocaNovaApiClient _apiClient;
    public RolesController(IVocaNovaApiClient apiClient) => _apiClient = apiClient;

    [HttpGet("/roles")]
    public async Task<IActionResult> Index(
        string? search, string? type, CancellationToken cancellationToken)
    {
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        type = string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToLowerInvariant();
        var roles = await _apiClient.GetRolesAsync(search, type, cancellationToken);
        return View(new RoleManagementViewModel
        {
            Roles = roles.Items,
            RolesUnavailable = roles.Items.Count == 0 && search is null && type is null,
            Search = search,
            Type = type,
            TotalRoles = roles.TotalItems,
        });
    }

    [HttpGet("/roles/admin-user-assignments")]
    public async Task<IActionResult> Assignments(
        uint? adminId,
        string? search,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _apiClient.GetAdminUserAssignmentsAsync(cancellationToken)
            ?? new Models.Api.SuperAdmin.AdminUserAssignmentOverview([], []);
        var selectedAdminId = assignments.Admins.Any(admin => admin.AdminId == adminId)
            ? adminId
            : assignments.Admins.FirstOrDefault()?.AdminId;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
        page = Math.Max(1, page);

        var filteredUsers = assignments.Users.AsEnumerable();
        if (search is not null)
        {
            filteredUsers = filteredUsers.Where(user =>
                user.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (user.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || user.UserId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        filteredUsers = status switch
        {
            "assigned" => filteredUsers.Where(user => user.AssignedAdminId == selectedAdminId),
            "unassigned" => filteredUsers.Where(user => user.AssignedAdminId is null),
            "other" => filteredUsers.Where(user =>
                user.AssignedAdminId.HasValue && user.AssignedAdminId != selectedAdminId),
            _ => filteredUsers,
        };

        const int pageSize = 30;
        var totalUsers = filteredUsers.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalUsers / (double)pageSize));
        page = Math.Min(page, totalPages);
        var users = filteredUsers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return View(new AdminUserAssignmentViewModel
        {
            Assignments = assignments,
            Users = users,
            SelectedAdminId = selectedAdminId,
            Search = search,
            Status = status,
            Page = page,
            PageSize = pageSize,
            TotalUsers = totalUsers,
        });
    }

    [HttpGet("/roles/create")]
    public IActionResult Create() => View(new SaveRoleViewModel());

    [HttpPost("/roles/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaveRoleViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _apiClient.CreateRoleAsync(model.RoleName!, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Could not create the role.");
            return View(model);
        }

        Feedback(result, "Role created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/roles/{roleId:uint}/edit")]
    public async Task<IActionResult> Edit(uint roleId, CancellationToken cancellationToken)
    {
        var roles = await _apiClient.GetRolesAsync(cancellationToken);
        var role = roles.Items.FirstOrDefault(item => item.RoleId == roleId);
        if (role is null) return NotFound();
        if (role.RoleName is "user" or "admin" or "super_admin")
        {
            TempData["RoleError"] = "System role names cannot be changed.";
            return RedirectToAction(nameof(Index));
        }
        return View(new SaveRoleViewModel { RoleId = role.RoleId, RoleName = role.RoleName });
    }

    [HttpPost("/roles/{roleId:uint}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(uint roleId, SaveRoleViewModel model, CancellationToken cancellationToken)
    {
        model.RoleId = roleId;
        if (!ModelState.IsValid) return View(model);

        var result = await _apiClient.UpdateRoleAsync(roleId, model.RoleName!, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Could not update the role.");
            return View(model);
        }

        Feedback(result, "Role updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/roles/{roleId:uint}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(uint roleId, CancellationToken cancellationToken)
    {
        Feedback(await _apiClient.DeleteRoleAsync(roleId, cancellationToken), "Role deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/roles/assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(uint roleId, uint userId, CancellationToken cancellationToken)
    {
        Feedback(await _apiClient.AssignRoleAsync(roleId, userId, cancellationToken), "Role assigned.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/roles/admin-user-assignments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssignments(
        uint adminId,
        uint[] userIds,
        uint[] visibleUserIds,
        string? search,
        string? status,
        int page,
        CancellationToken cancellationToken)
    {
        var assignments = await _apiClient.GetAdminUserAssignmentsAsync(cancellationToken)
            ?? new Models.Api.SuperAdmin.AdminUserAssignmentOverview([], []);
        var existingUserIds = assignments.Users
            .Where(user => user.AssignedAdminId == adminId)
            .Select(user => user.UserId);
        var updatedUserIds = existingUserIds
            .Except(visibleUserIds)
            .Concat(userIds)
            .Distinct()
            .ToArray();

        var result = await _apiClient.SaveAdminUserAssignmentsAsync(adminId, updatedUserIds, cancellationToken);
        Feedback(result, "Assignments saved.");
        return RedirectToAction(nameof(Assignments), new { adminId, search, status, page });
    }

    private void Feedback(ApiActionResult result, string success)
    {
        TempData[result.IsSuccess ? "RoleSuccess" : "RoleError"] =
            result.IsSuccess ? success : result.Message ?? "The operation failed.";
    }
}
