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

    private void Feedback(ApiActionResult result, string success)
    {
        TempData[result.IsSuccess ? "RoleSuccess" : "RoleError"] =
            result.IsSuccess ? success : result.Message ?? "The operation failed.";
    }
}
