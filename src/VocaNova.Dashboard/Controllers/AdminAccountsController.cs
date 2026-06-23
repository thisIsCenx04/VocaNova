using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using VocaNova.Dashboard.Models.Api.AdminAccounts;
using VocaNova.Dashboard.Models.AdminAccounts;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// FD-04 — chỉ super_admin (R3). UI dựng đầy đủ theo contract G9; chưa có API → tự hiện "Sắp có".
[Authorize(Roles = "super_admin")]
public sealed class AdminAccountsController : Controller
{
    private const string BasePath = "api/admin/admin-accounts";
    private static readonly IReadOnlySet<string> ValidRoles =
        new HashSet<string>(StringComparer.Ordinal) { "admin", "super_admin" };

    private readonly IVocaNovaApiClient _api;
    private readonly IStringLocalizer<SharedResource> _l;

    public AdminAccountsController(IVocaNovaApiClient api, IStringLocalizer<SharedResource> l)
    {
        _api = api;
        _l = l;
    }

    [HttpGet("/admin-accounts")]
    public async Task<IActionResult> Index([FromQuery] AdminAccountListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            query.Page = 1;
        }

        var accounts = await _api.GetPagedAsync<AdminAccountDto>(BuildUrl(query), cancellationToken);
        // G9 chưa deploy → 404. Khi đó vẫn render trang nhưng ở trạng thái "Sắp có".
        var apiAvailable = accounts.StatusCode != 404;

        return View(new AdminAccountListViewModel
        {
            Query = query,
            Accounts = accounts.IsSuccess ? accounts.Items : Array.Empty<AdminAccountDto>(),
            Loaded = accounts.IsSuccess || !apiAvailable,
            Pagination = accounts.Pagination,
            ApiAvailable = apiAvailable,
        });
    }

    [HttpGet("/admin-accounts/create")]
    public IActionResult CreateForm() => PartialView("_AdminAccountForm", new AdminAccountFormViewModel());

    [HttpPost("/admin-accounts/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] AdminAccountFormViewModel input, CancellationToken cancellationToken)
    {
        var error = Validate(input, isCreate: true);
        if (error is not null)
        {
            input.Error = error;
            return PartialView("_AdminAccountForm", input);
        }

        var result = await _api.PostAsync<AdminAccountDto>(BasePath, new
        {
            phone = input.Phone?.Trim(),
            display_name = input.DisplayName?.Trim(),
            password = input.Password,
            role = input.Role,
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            input.Error = ErrorMessage(result);
            return PartialView("_AdminAccountForm", input);
        }

        return Json(new { message = _l["AdminAcc.Toast.Created"].Value });
    }

    [HttpGet("/admin-accounts/{id:long}/edit")]
    public async Task<IActionResult> EditForm(uint id, CancellationToken cancellationToken)
    {
        var account = await _api.GetAsync<AdminAccountDto>($"{BasePath}/{id}", cancellationToken);
        if (!account.IsSuccess || account.Data is null)
        {
            return PartialView("_AdminAccountForm", new AdminAccountFormViewModel { Id = id, Error = _l["AdminAcc.NotFound"].Value });
        }

        return PartialView("_AdminAccountForm", new AdminAccountFormViewModel
        {
            Id = account.Data.UserId,
            Phone = account.Data.Phone,
            DisplayName = account.Data.DisplayName,
            Role = account.Data.Role,
        });
    }

    [HttpPost("/admin-accounts/{id:long}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(uint id, [FromForm] AdminAccountFormViewModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        var error = Validate(input, isCreate: false);
        if (error is not null)
        {
            input.Error = error;
            return PartialView("_AdminAccountForm", input);
        }

        var result = await _api.PutAsync<AdminAccountDto>($"{BasePath}/{id}", new
        {
            display_name = input.DisplayName?.Trim(),
            role = input.Role,
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            input.Error = ErrorMessage(result);
            return PartialView("_AdminAccountForm", input);
        }

        return Json(new { message = _l["AdminAcc.Toast.Updated"].Value });
    }

    [HttpPost("/admin-accounts/{id:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.DeleteAsync<object>($"{BasePath}/{id}", cancellationToken);
        SetToast(result.IsSuccess, "AdminAcc.Toast.Deleted", result.Message);
        return RedirectToAction(nameof(Index));
    }

    // Mirror contract: phone VN, display_name 2–150, password StrongPassword (chỉ khi tạo), role hợp lệ.
    private string? Validate(AdminAccountFormViewModel m, bool isCreate)
    {
        if (isCreate && (string.IsNullOrWhiteSpace(m.Phone) || !Regex.IsMatch(m.Phone, @"^0[3-9]\d{8}$")))
        {
            return _l["AdminAcc.Validation.Phone"].Value;
        }
        var name = m.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 150)
        {
            return _l["AdminAcc.Validation.Name"].Value;
        }
        if (isCreate)
        {
            var p = m.Password;
            if (string.IsNullOrEmpty(p) || p.Length < 8 || !p.Any(char.IsUpper) || !p.Any(char.IsLower) || !p.Any(char.IsDigit))
            {
                return _l["AdminAcc.Validation.Password"].Value;
            }
        }
        if (!ValidRoles.Contains(m.Role))
        {
            return _l["AdminAcc.Validation.Role"].Value;
        }

        return null;
    }

    private static string BuildUrl(AdminAccountListQuery q)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = q.Page.ToString(),
            ["limit"] = q.Limit.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            parameters["q"] = q.Q;
        }

        return QueryHelpers.AddQueryString(BasePath, parameters);
    }

    private string ErrorMessage(ApiResult<AdminAccountDto> result)
    {
        // 404 = endpoint G9 chưa deploy.
        if (result.StatusCode == 404)
        {
            return _l["AdminAcc.ComingSoon"].Value;
        }

        return string.IsNullOrWhiteSpace(result.Message) ? _l["Toast.ActionFailed"].Value : result.Message!;
    }

    private void SetToast(bool success, string successKey, string? errorMessage)
    {
        TempData["ToastKind"] = success ? "ok" : "err";
        TempData["ToastMessage"] = success
            ? _l[successKey].Value
            : (string.IsNullOrWhiteSpace(errorMessage) ? _l["Toast.ActionFailed"].Value : errorMessage);
    }
}
