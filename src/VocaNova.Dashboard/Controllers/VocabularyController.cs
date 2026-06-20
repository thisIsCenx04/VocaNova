using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using VocaNova.Dashboard.Models.Api.Dictionary;
using VocaNova.Dashboard.Models.Vocabulary;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class VocabularyController : Controller
{
    private readonly IVocaNovaApiClient _api;
    private readonly IStringLocalizer<SharedResource> _l;

    public VocabularyController(IVocaNovaApiClient api, IStringLocalizer<SharedResource> l)
    {
        _api = api;
        _l = l;
    }

    [HttpGet("/vocabulary")]
    public async Task<IActionResult> Index([FromQuery] VocabularyListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            query.Page = 1;
        }

        var topics = await _api.GetAsync<List<TopicSummaryDto>>("api/topics", cancellationToken);

        // Ưu tiên admin word list (G1, có status + includeDeleted); nếu chưa có (404) → fallback public.
        var adminAvailable = true;
        var words = await _api.GetPagedAsync<WordSummaryDto>(BuildWordsUrl(query, admin: true), cancellationToken);
        if (words.StatusCode == 404)
        {
            adminAvailable = false;
            words = await _api.GetPagedAsync<WordSummaryDto>(BuildWordsUrl(query, admin: false), cancellationToken);
        }

        var model = new VocabularyListViewModel
        {
            Query = query,
            Words = words.IsSuccess ? words.Items : Array.Empty<WordSummaryDto>(),
            Loaded = words.IsSuccess,
            Pagination = words.Pagination,
            Topics = topics.IsSuccess && topics.Data is not null ? topics.Data : new List<TopicSummaryDto>(),
            AdminListAvailable = adminAvailable,
        };

        return View(model);
    }

    [HttpPost("/vocabulary/{id:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.DeleteAsync<object>($"api/admin/words/{id}", cancellationToken);
        SetToast(result.IsSuccess, "Toast.WordDeleted", result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/vocabulary/{id:long}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.PatchAsync<object>($"api/admin/words/{id}/restore", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Toast.WordRestored", result.Message);
        return RedirectToAction(nameof(Index));
    }

    private void SetToast(bool success, string successKey, string? errorMessage)
    {
        // Key sẽ được view dịch khi hiển thị; ở đây chỉ truyền message thô qua TempData.
        TempData["ToastKind"] = success ? "ok" : "err";
        TempData["ToastMessage"] = success
            ? _l[successKey].Value
            : (string.IsNullOrWhiteSpace(errorMessage) ? _l["Toast.ActionFailed"].Value : errorMessage);
    }

    private static string BuildWordsUrl(VocabularyListQuery q, bool admin)
    {
        var basePath = admin ? "api/admin/words" : "api/words";
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = q.Page.ToString(),
            ["limit"] = q.Limit.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            parameters["q"] = q.Q;
        }
        if (!string.IsNullOrWhiteSpace(q.Cefr))
        {
            parameters["cefr"] = q.Cefr;
        }
        if (q.TopicId is > 0)
        {
            parameters["topicId"] = q.TopicId.Value.ToString();
        }
        if (admin && !string.IsNullOrWhiteSpace(q.Status))
        {
            parameters["status"] = q.Status;
        }
        if (admin && q.IncludeDeleted)
        {
            parameters["includeDeleted"] = "true";
        }

        return QueryHelpers.AddQueryString(basePath, parameters);
    }
}
