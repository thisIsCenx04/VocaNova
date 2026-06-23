using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using VocaNova.Dashboard.Models.Api.Knn;
using VocaNova.Dashboard.Models.Knn;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class KnnController : Controller
{
    private static readonly Regex RegionCodePattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    private readonly IVocaNovaApiClient _api;
    private readonly IStringLocalizer<SharedResource> _l;

    public KnnController(IVocaNovaApiClient api, IStringLocalizer<SharedResource> l)
    {
        _api = api;
        _l = l;
    }

    // ── Overview: config read-only + rebuild status/trigger ──────────────
    [HttpGet("/knn")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var config = await _api.GetAsync<KnnConfigDto>("api/admin/knn/config", cancellationToken);
        var status = await _api.GetAsync<KnnRebuildStatusDto>("api/admin/knn/rebuild-status", cancellationToken);

        return View(new KnnOverviewViewModel
        {
            Config = config.IsSuccess ? config.Data : null,
            ConfigLoaded = config.IsSuccess && config.Data is not null,
            RebuildStatus = status.IsSuccess ? status.Data : null,
            StatusLoaded = status.IsSuccess && status.Data is not null,
        });
    }

    [HttpPost("/knn/trigger-rebuild")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TriggerRebuild(CancellationToken cancellationToken)
    {
        // API trả 202 (queued) hoặc 429 (rate limit) → message hiện qua toast.
        var result = await _api.PostAsync<object>("api/admin/knn/trigger-rebuild", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Knn.Toast.RebuildQueued", result.Message);
        return RedirectToAction(nameof(Overview));
    }

    // ── List ─────────────────────────────────────────────────────────────
    [HttpGet("/knn/{type}")]
    public async Task<IActionResult> Index(string type, [FromQuery] KnnListQuery query, CancellationToken cancellationToken)
    {
        var descriptor = KnnTypes.Find(type);
        if (descriptor is null)
        {
            return NotFound();
        }

        if (query.Page < 1)
        {
            query.Page = 1;
        }

        var items = await _api.GetPagedAsync<KnnItemDto>(BuildListUrl(descriptor, query), cancellationToken);

        return View(new KnnListViewModel
        {
            Type = descriptor,
            Query = query,
            Items = items.IsSuccess ? items.Items : Array.Empty<KnnItemDto>(),
            Loaded = items.IsSuccess,
            Pagination = items.Pagination,
        });
    }

    // ── Create / Edit (AJAX _FormModal) ──────────────────────────────────
    [HttpGet("/knn/{type}/create")]
    public async Task<IActionResult> CreateForm(string type, CancellationToken cancellationToken)
    {
        var descriptor = KnnTypes.Find(type);
        if (descriptor is null)
        {
            return NotFound();
        }

        return PartialView("_KnnForm", new KnnFormViewModel
        {
            Type = descriptor,
            ParentOptions = await LoadParentOptionsAsync(descriptor, cancellationToken),
        });
    }

    [HttpPost("/knn/{type}/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string type, [FromForm] KnnFormViewModel input, CancellationToken cancellationToken)
    {
        var descriptor = KnnTypes.Find(type);
        if (descriptor is null)
        {
            return NotFound();
        }

        input.Type = descriptor;
        var error = Validate(descriptor, input);
        if (error is not null)
        {
            return await FormError(descriptor, input, error, cancellationToken);
        }

        var result = await _api.PostAsync<KnnItemDto>($"api/admin/knn/{descriptor.Key}", ToRequest(descriptor, input), cancellationToken);
        if (!result.IsSuccess)
        {
            return await FormError(descriptor, input, ErrorMessage(result.Message), cancellationToken);
        }

        return Json(new { message = _l["Knn.Toast.Created"].Value });
    }

    [HttpGet("/knn/{type}/{id:long}/edit")]
    public async Task<IActionResult> EditForm(string type, uint id, CancellationToken cancellationToken)
    {
        var descriptor = KnnTypes.Find(type);
        if (descriptor is null)
        {
            return NotFound();
        }

        var item = await _api.GetAsync<KnnItemDto>($"api/admin/knn/{descriptor.Key}/{id}?includeDeleted=true", cancellationToken);
        if (!item.IsSuccess || item.Data is null)
        {
            return PartialView("_KnnForm", new KnnFormViewModel { Type = descriptor, Id = id, Error = _l["Knn.NotFound"].Value });
        }

        var d = item.Data;
        return PartialView("_KnnForm", new KnnFormViewModel
        {
            Type = descriptor,
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            ParentId = d.ParentId,
            MinAge = d.MinAge,
            MaxAge = d.MaxAge,
            DisplayOrder = d.DisplayOrder,
            Description = d.Description,
            ParentOptions = await LoadParentOptionsAsync(descriptor, cancellationToken, excludeId: d.Id),
        });
    }

    [HttpPost("/knn/{type}/{id:long}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string type, uint id, [FromForm] KnnFormViewModel input, CancellationToken cancellationToken)
    {
        var descriptor = KnnTypes.Find(type);
        if (descriptor is null)
        {
            return NotFound();
        }

        input.Type = descriptor;
        input.Id = id;
        var error = Validate(descriptor, input);
        if (error is not null)
        {
            return await FormError(descriptor, input, error, cancellationToken);
        }

        var result = await _api.PutAsync<KnnItemDto>($"api/admin/knn/{descriptor.Key}/{id}", ToRequest(descriptor, input), cancellationToken);
        if (!result.IsSuccess)
        {
            return await FormError(descriptor, input, ErrorMessage(result.Message), cancellationToken);
        }

        return Json(new { message = _l["Knn.Toast.Updated"].Value });
    }

    // ── Delete / Restore (confirm modal) ─────────────────────────────────
    [HttpPost("/knn/{type}/{id:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string type, uint id, CancellationToken cancellationToken)
    {
        var descriptor = KnnTypes.Find(type);
        if (descriptor is null)
        {
            return NotFound();
        }

        // API trả 409 nếu lookup đang được dùng → message hiển thị qua toast.
        var result = await _api.DeleteAsync<object>($"api/admin/knn/{descriptor.Key}/{id}", cancellationToken);
        SetToast(result.IsSuccess, "Knn.Toast.Deleted", result.Message);
        return RedirectToAction(nameof(Index), new { type = descriptor.Key });
    }

    [HttpPost("/knn/{type}/{id:long}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string type, uint id, CancellationToken cancellationToken)
    {
        var descriptor = KnnTypes.Find(type);
        if (descriptor is null)
        {
            return NotFound();
        }

        var result = await _api.PatchAsync<object>($"api/admin/knn/{descriptor.Key}/{id}/restore", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Knn.Toast.Restored", result.Message);
        return RedirectToAction(nameof(Index), new { type = descriptor.Key });
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private async Task<IReadOnlyList<KnnItemDto>> LoadParentOptionsAsync(KnnTypeDescriptor descriptor, CancellationToken cancellationToken, uint? excludeId = null)
    {
        if (!descriptor.HasParent)
        {
            return Array.Empty<KnnItemDto>();
        }

        // Lấy region active để chọn parent. Loại chính nó để tránh tự tham chiếu (cycle do backend chặn sâu hơn).
        var regions = await _api.GetPagedAsync<KnnItemDto>("api/admin/knn/regions?limit=200", cancellationToken);
        if (!regions.IsSuccess)
        {
            return Array.Empty<KnnItemDto>();
        }

        return regions.Items.Where(r => excludeId is null || r.Id != excludeId).ToList();
    }

    private async Task<IActionResult> FormError(KnnTypeDescriptor descriptor, KnnFormViewModel input, string error, CancellationToken cancellationToken)
    {
        input.Error = error;
        input.ParentOptions = await LoadParentOptionsAsync(descriptor, cancellationToken, excludeId: input.Id);
        return PartialView("_KnnForm", input);
    }

    private object ToRequest(KnnTypeDescriptor d, KnnFormViewModel m)
    {
        var name = m.Name?.Trim();
        return d.Key switch
        {
            "age-ranges" => new { name, min_age = m.MinAge, max_age = m.MaxAge, display_order = m.DisplayOrder },
            "regions" => new { name, code = m.Code?.Trim(), parent_id = m.ParentId },
            "occupations" => new { name, description = Clean(m.Description) },
            "education-levels" => new { name, description = Clean(m.Description), display_order = m.DisplayOrder },
            "learning-purposes" => new { name, description = Clean(m.Description) },
            _ => new { name },
        };
    }

    // Mirror backend validators (AdminKnnLookupValidators).
    private string? Validate(KnnTypeDescriptor d, KnnFormViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Name))
        {
            return _l["Knn.Validation.NameRequired"].Value;
        }
        if (m.Name.Trim().Length > d.NameMaxLength)
        {
            return _l["Knn.Validation.NameMax", d.NameMaxLength].Value;
        }

        if (d.HasCode)
        {
            var code = m.Code?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return _l["Knn.Validation.CodeRequired"].Value;
            }
            if (code.Length > 10)
            {
                return _l["Knn.Validation.CodeMax"].Value;
            }
            if (!RegionCodePattern.IsMatch(code))
            {
                return _l["Knn.Validation.CodePattern"].Value;
            }
        }

        if (d.HasAge)
        {
            if (m.MinAge is < 0 || m.MaxAge is < 0)
            {
                return _l["Knn.Validation.AgeNonNegative"].Value;
            }
            if (m.MinAge.HasValue && m.MaxAge.HasValue && m.MinAge > m.MaxAge)
            {
                return _l["Knn.Validation.AgeOrder"].Value;
            }
        }

        if (d.HasDescription && !string.IsNullOrWhiteSpace(m.Description) && m.Description.Trim().Length > 255)
        {
            return _l["Knn.Validation.DescriptionMax"].Value;
        }

        if (d.HasDisplayOrder && m.DisplayOrder < 0)
        {
            return _l["Knn.Validation.OrderNonNegative"].Value;
        }

        return null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildListUrl(KnnTypeDescriptor d, KnnListQuery q)
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
        if (q.IncludeDeleted)
        {
            parameters["include_deleted"] = "true";
        }

        return QueryHelpers.AddQueryString($"api/admin/knn/{d.Key}", parameters);
    }

    private string ErrorMessage(string? apiMessage)
        => string.IsNullOrWhiteSpace(apiMessage) ? _l["Toast.ActionFailed"].Value : apiMessage;

    private void SetToast(bool success, string successKey, string? errorMessage)
    {
        TempData["ToastKind"] = success ? "ok" : "err";
        TempData["ToastMessage"] = success
            ? _l[successKey].Value
            : (string.IsNullOrWhiteSpace(errorMessage) ? _l["Toast.ActionFailed"].Value : errorMessage);
    }
}
