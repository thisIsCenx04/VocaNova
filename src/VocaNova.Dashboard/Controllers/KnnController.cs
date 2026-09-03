using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Data.Dtos.Knn;
using VocaNova.Dashboard.Models.Knn;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F063 — KNN Management. Overview (config + rebuild) + 5 lookup CRUD pages dùng chung 1 view generic.
public sealed class KnnController : Controller
{
    private const int DefaultPageSize = 10;
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100];

    private static readonly IReadOnlyList<KnnLookupLink> Lookups = new[]
    {
        new KnnLookupLink("age-ranges", "Age Ranges", "name, min/max age, display order"),
        new KnnLookupLink("regions", "Regions", "name, code, parent"),
        new KnnLookupLink("occupations", "Occupations", "name, description"),
        new KnnLookupLink("education-levels", "Education Levels", "name, description, display order"),
        new KnnLookupLink("learning-purposes", "Learning Purposes", "name, description"),
    };

    private readonly IVocaNovaApiClient _apiClient;

    public KnnController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/knn")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var config = await _apiClient.GetKnnConfigAsync(cancellationToken);
        var status = await _apiClient.GetKnnRebuildStatusAsync(cancellationToken);

        return View(new KnnOverviewViewModel
        {
            Config = config,
            RebuildStatus = status,
            Lookups = Lookups,
        });
    }

    [HttpPost("/knn/trigger-rebuild")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TriggerRebuild(CancellationToken cancellationToken)
    {
        var result = await _apiClient.TriggerKnnRebuildAsync(cancellationToken);
        if (result.IsSuccess)
        {
            TempData["KnnSuccess"] = "Rebuild queued. Please wait a few moments.";
        }
        else
        {
            TempData["KnnError"] = result.StatusCode switch
            {
                429 => "Rebuild was triggered recently. Try again later (1 request / 5 min).",
                401 or 403 => "You do not have permission to trigger a rebuild.",
                _ => result.Message ?? "Unable to trigger rebuild.",
            };
        }

        return RedirectToAction(nameof(Index));
    }

    // Trọng số vector KNN: sửa trực tiếp trên dashboard, có hiệu lực ngay cho lần tính tiếp theo.
    [HttpPost("/knn/vector-weights")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveVectorWeights(
        IFormCollection form,
        CancellationToken cancellationToken)
    {
        var weights = new KnnVectorWeights(
            Dbl(form, "age_range_weight"),
            Dbl(form, "region_weight"),
            Dbl(form, "occupation_weight"),
            Dbl(form, "education_level_weight"),
            Dbl(form, "learning_purpose_weight"),
            Dbl(form, "interest_topics_weight"));

        var result = await _apiClient.UpdateKnnVectorWeightsAsync(weights, cancellationToken);
        SetFeedback(result, "Vector weights saved. New weights apply to the next recommendation computed.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/knn/vector-weights/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetVectorWeights(CancellationToken cancellationToken)
    {
        var result = await _apiClient.ResetKnnVectorWeightsAsync(cancellationToken);
        SetFeedback(result, "Vector weights reset to the built-in defaults.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/knn/{slug}")]
    public async Task<IActionResult> Lookup(
        string slug,
        string? q,
        string? status,
        bool includeDeleted = false,
        int page = 1,
        int limit = DefaultPageSize,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidSlug(slug))
        {
            return NotFound();
        }

        if (page < 1)
        {
            page = 1;
        }

        if (!AllowedPageSizes.Contains(limit))
        {
            limit = DefaultPageSize;
        }

        var filter = new KnnLookupFilter(
            string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            string.IsNullOrWhiteSpace(status) ? null : status,
            includeDeleted,
            page,
            limit,
            // Danh sách phân trang phía server nên sort do API xử lý.
            string.IsNullOrWhiteSpace(sortBy) ? null : sortBy,
            string.IsNullOrWhiteSpace(sortDirection) ? null : sortDirection);

        IReadOnlyList<KnnLookupRow> rows;
        int totalItems;
        int totalPages;

        switch (slug)
        {
            case "age-ranges":
            {
                var data = await _apiClient.GetKnnLookupPageAsync<AgeRange>(slug, filter, cancellationToken);
                rows = data.Items.Select(MapAgeRange).ToArray();
                totalItems = data.TotalItems;
                totalPages = data.TotalPages;
                break;
            }
            case "regions":
            {
                var data = await _apiClient.GetKnnLookupPageAsync<Region>(slug, filter, cancellationToken);
                rows = data.Items.Select(MapRegion).ToArray();
                totalItems = data.TotalItems;
                totalPages = data.TotalPages;
                break;
            }
            case "occupations":
            {
                var data = await _apiClient.GetKnnLookupPageAsync<Occupation>(slug, filter, cancellationToken);
                rows = data.Items.Select(MapOccupation).ToArray();
                totalItems = data.TotalItems;
                totalPages = data.TotalPages;
                break;
            }
            case "education-levels":
            {
                var data = await _apiClient.GetKnnLookupPageAsync<EducationLevel>(slug, filter, cancellationToken);
                rows = data.Items.Select(MapEducationLevel).ToArray();
                totalItems = data.TotalItems;
                totalPages = data.TotalPages;
                break;
            }
            default: // learning-purposes
            {
                var data = await _apiClient.GetKnnLookupPageAsync<LearningPurpose>(slug, filter, cancellationToken);
                rows = data.Items.Select(MapLearningPurpose).ToArray();
                totalItems = data.TotalItems;
                totalPages = data.TotalPages;
                break;
            }
        }

        var model = new KnnLookupViewModel
        {
            Slug = slug,
            Title = TitleFor(slug),
            Columns = ColumnsFor(slug),
            AddFields = FieldTemplate(slug),
            Rows = rows,
            Q = filter.Q,
            Status = filter.Status,
            IncludeDeleted = includeDeleted,
            Page = page,
            PageSize = limit,
            TotalItems = totalItems,
            TotalPages = totalPages,
            SortBy = filter.SortBy,
            SortDirection = filter.SortDirection,
        };

        return View(model);
    }

    [HttpPost("/knn/{slug}/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        string slug,
        uint id,
        string? returnUrl,
        IFormCollection form,
        CancellationToken cancellationToken)
    {
        if (!IsValidSlug(slug))
        {
            return NotFound();
        }

        var payload = BuildPayload(slug, form);
        var result = id == 0
            ? await _apiClient.CreateKnnLookupAsync(slug, payload, cancellationToken)
            : await _apiClient.UpdateKnnLookupAsync(slug, id, payload, cancellationToken);

        SetFeedback(result, id == 0 ? "Created." : "Updated.");
        return RedirectBack(returnUrl, slug);
    }

    [HttpPost("/knn/{slug}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string slug, uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!IsValidSlug(slug))
        {
            return NotFound();
        }

        var result = await _apiClient.DeleteKnnLookupAsync(slug, id, cancellationToken);
        SetFeedback(result, "Deleted.");
        return RedirectBack(returnUrl, slug);
    }

    [HttpPost("/knn/{slug}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string slug, uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!IsValidSlug(slug))
        {
            return NotFound();
        }

        var result = await _apiClient.RestoreKnnLookupAsync(slug, id, cancellationToken);
        SetFeedback(result, "Restored.");
        return RedirectBack(returnUrl, slug);
    }

    // ---- mapping ----

    private static KnnLookupRow MapAgeRange(AgeRange a) => new(
        a.AgeRangeId,
        a.Status,
        new[] { a.Name, a.MinAge?.ToString(CultureInfo.InvariantCulture) ?? "—", a.MaxAge?.ToString(CultureInfo.InvariantCulture) ?? "—", a.DisplayOrder.ToString(CultureInfo.InvariantCulture) },
        new[]
        {
            new KnnFieldDef("name", "Name", "text", a.Name, true),
            new KnnFieldDef("min_age", "Min age", "number", a.MinAge?.ToString(CultureInfo.InvariantCulture)),
            new KnnFieldDef("max_age", "Max age", "number", a.MaxAge?.ToString(CultureInfo.InvariantCulture)),
            new KnnFieldDef("display_order", "Order", "number", a.DisplayOrder.ToString(CultureInfo.InvariantCulture)),
        });

    private static KnnLookupRow MapRegion(Region r) => new(
        r.RegionId,
        r.Status,
        new[] { r.Name, r.Code, r.ParentName ?? "—" },
        new[]
        {
            new KnnFieldDef("name", "Name", "text", r.Name, true),
            new KnnFieldDef("code", "Code", "text", r.Code, true),
            new KnnFieldDef("parent_id", "Parent ID", "number", r.ParentId?.ToString(CultureInfo.InvariantCulture)),
        });

    private static KnnLookupRow MapOccupation(Occupation o) => new(
        o.OccupationId,
        o.Status,
        new[] { o.Name, o.Description ?? "—" },
        new[]
        {
            new KnnFieldDef("name", "Name", "text", o.Name, true),
            new KnnFieldDef("description", "Description", "text", o.Description),
        });

    private static KnnLookupRow MapEducationLevel(EducationLevel e) => new(
        e.EducationLevelId,
        e.Status,
        new[] { e.Name, e.Description ?? "—", e.DisplayOrder.ToString(CultureInfo.InvariantCulture) },
        new[]
        {
            new KnnFieldDef("name", "Name", "text", e.Name, true),
            new KnnFieldDef("description", "Description", "text", e.Description),
            new KnnFieldDef("display_order", "Order", "number", e.DisplayOrder.ToString(CultureInfo.InvariantCulture)),
        });

    private static KnnLookupRow MapLearningPurpose(LearningPurpose l) => new(
        l.LearningPurposeId,
        l.Status,
        new[] { l.Name, l.Description ?? "—" },
        new[]
        {
            new KnnFieldDef("name", "Name", "text", l.Name, true),
            new KnnFieldDef("description", "Description", "text", l.Description),
        });

    // Anonymous payload → snake_case (ApiJson policy) khớp request DTO của API.
    private static object BuildPayload(string slug, IFormCollection form) => slug switch
    {
        "age-ranges" => new
        {
            Name = Str(form, "name"),
            MinAge = IntN(form, "min_age"),
            MaxAge = IntN(form, "max_age"),
            DisplayOrder = Int(form, "display_order"),
        },
        "regions" => new
        {
            Name = Str(form, "name"),
            Code = Str(form, "code"),
            ParentId = UIntN(form, "parent_id"),
        },
        "occupations" => new
        {
            Name = Str(form, "name"),
            Description = Str(form, "description"),
        },
        "education-levels" => new
        {
            Name = Str(form, "name"),
            Description = Str(form, "description"),
            DisplayOrder = Int(form, "display_order"),
        },
        _ => new
        {
            Name = Str(form, "name"),
            Description = Str(form, "description"),
        },
    };

    private static string TitleFor(string slug) =>
        Lookups.FirstOrDefault(l => l.Slug == slug)?.Title ?? slug;

    // Khoá sort phải khớp SortFields của KnnLookupQueryValidator bên API; cột không có khoá
    // (ví dụ Parent) sẽ render như tiêu đề tĩnh.
    private static IReadOnlyList<KnnColumn> ColumnsFor(string slug) => slug switch
    {
        "age-ranges" =>
        [
            new("Name", "name"),
            new("Min age", "min_age"),
            new("Max age", "max_age"),
            new("Order", "display_order"),
        ],
        "regions" =>
        [
            new("Name", "name"),
            new("Code", "code"),
            new("Parent", "parent"),
        ],
        "education-levels" =>
        [
            new("Name", "name"),
            new("Description", "description"),
            new("Order", "display_order"),
        ],
        // occupations, learning-purposes
        _ =>
        [
            new("Name", "name"),
            new("Description", "description"),
        ],
    };

    private static IReadOnlyList<KnnFieldDef> FieldTemplate(string slug) => slug switch
    {
        "age-ranges" => new[]
        {
            new KnnFieldDef("name", "Name", "text", null, true),
            new KnnFieldDef("min_age", "Min age", "number"),
            new KnnFieldDef("max_age", "Max age", "number"),
            new KnnFieldDef("display_order", "Order", "number", "0"),
        },
        "regions" => new[]
        {
            new KnnFieldDef("name", "Name", "text", null, true),
            new KnnFieldDef("code", "Code", "text", null, true),
            new KnnFieldDef("parent_id", "Parent ID", "number"),
        },
        "education-levels" => new[]
        {
            new KnnFieldDef("name", "Name", "text", null, true),
            new KnnFieldDef("description", "Description", "text"),
            new KnnFieldDef("display_order", "Order", "number", "0"),
        },
        _ => new[]
        {
            new KnnFieldDef("name", "Name", "text", null, true),
            new KnnFieldDef("description", "Description", "text"),
        },
    };

    private static bool IsValidSlug(string slug) => Lookups.Any(l => l.Slug == slug);

    private static string? Str(IFormCollection form, string key)
    {
        var v = form[key].ToString();
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    private static int Int(IFormCollection form, string key) =>
        int.TryParse(form[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static int? IntN(IFormCollection form, string key) =>
        int.TryParse(form[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static uint? UIntN(IFormCollection form, string key) =>
        uint.TryParse(form[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static double Dbl(IFormCollection form, string key) =>
        double.TryParse(form[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private void SetFeedback(ApiActionResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            TempData["KnnSuccess"] = successMessage;
            return;
        }

        TempData["KnnError"] = result.StatusCode switch
        {
            400 => result.Message ?? "Validation failed.",
            401 or 403 => "You do not have permission to perform this action.",
            404 => "Item not found.",
            409 => result.Message ?? "Conflict: duplicate name/code or invalid reference.",
            _ => result.Message ?? "The request could not be completed.",
        };
    }

    private IActionResult RedirectBack(string? returnUrl, string slug)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Lookup), new { slug });
    }
}
