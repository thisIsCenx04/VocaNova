using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Vocabulary;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F057 — Vocabulary List & Filter. Phân trang server-side; xóa/khôi phục proxy qua API rồi redirect giữ nguyên bộ lọc.
public sealed class VocabularyController : Controller
{
    private const int PageSize = 20;

    private readonly IVocaNovaApiClient _apiClient;

    public VocabularyController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/vocabulary")]
    public async Task<IActionResult> Index(
        string? q,
        string? cefr,
        uint? topicId,
        string? status,
        bool includeDeleted = false,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        var filter = new WordListFilter(
            Q: string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            Cefr: string.IsNullOrWhiteSpace(cefr) ? null : cefr,
            TopicId: topicId,
            Status: string.IsNullOrWhiteSpace(status) ? null : status,
            IncludeDeleted: includeDeleted,
            Page: page,
            Limit: PageSize);

        var words = await _apiClient.GetWordsAsync(filter, cancellationToken);
        var topics = await _apiClient.GetTopicsAsync(cancellationToken);

        var model = new VocabularyListViewModel
        {
            Items = words.Items,
            Topics = topics,
            Q = filter.Q,
            Cefr = filter.Cefr,
            TopicId = topicId,
            Status = filter.Status,
            IncludeDeleted = includeDeleted,
            Page = words.Page,
            Limit = words.Limit,
            TotalItems = words.TotalItems,
            TotalPages = words.TotalPages,
        };

        return View(model);
    }

    [HttpPost("/vocabulary/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.DeleteWordAsync(id, cancellationToken);
        SetActionFeedback(result, "Word deleted.", "delete");
        return RedirectBack(returnUrl);
    }

    [HttpPost("/vocabulary/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.RestoreWordAsync(id, cancellationToken);
        SetActionFeedback(result, "Word restored.", "restore");
        return RedirectBack(returnUrl);
    }

    // ---- F058: Detail page + inline AJAX management ----

    [HttpGet("/vocabulary/{id:uint}")]
    public async Task<IActionResult> Detail(uint id, CancellationToken cancellationToken)
    {
        var detail = await _apiClient.GetWordDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    [HttpPost("/vocabulary/{id:uint}/senses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSense(
        uint id,
        [FromForm] SenseInput input,
        CancellationToken cancellationToken)
    {
        var result = await _apiClient.CreateSenseAsync(id, input, cancellationToken);
        return JsonResultFor(result, "Sense added.");
    }

    [HttpPost("/vocabulary/{id:uint}/senses/{senseId:uint}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSense(
        uint id,
        uint senseId,
        [FromForm] SenseInput input,
        CancellationToken cancellationToken)
    {
        var result = await _apiClient.UpdateSenseAsync(id, senseId, input, cancellationToken);
        return JsonResultFor(result, "Sense updated.");
    }

    [HttpPost("/vocabulary/{id:uint}/audio")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAudio(
        uint id,
        string accent,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Json(new { success = false, message = "Please choose an audio file." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _apiClient.UploadAudioAsync(
            id,
            new AudioUpload(accent, stream, file.FileName, file.ContentType),
            cancellationToken);
        return JsonResultFor(result, "Audio uploaded.");
    }

    [HttpPost("/vocabulary/{id:uint}/audio/{audioId:uint}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAudio(uint id, uint audioId, CancellationToken cancellationToken)
    {
        var result = await _apiClient.DeleteAudioAsync(id, audioId, cancellationToken);
        return JsonResultFor(result, "Audio deleted.");
    }

    [HttpPost("/vocabulary/{id:uint}/image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(
        uint id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Json(new { success = false, message = "Please choose an image file." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _apiClient.UploadImageAsync(
            id,
            new ImageUpload(stream, file.FileName, file.ContentType),
            cancellationToken);
        return JsonResultFor(result, "Image uploaded.");
    }

    // Chuẩn hóa kết quả AJAX: { success, message }.
    private IActionResult JsonResultFor(ApiActionResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            return Json(new { success = true, message = successMessage });
        }

        var message = result.StatusCode switch
        {
            401 or 403 => "You do not have permission to perform this action.",
            404 => "Resource not found.",
            _ => result.Message ?? "The request could not be completed.",
        };
        return Json(new { success = false, message });
    }

    private void SetActionFeedback(ApiActionResult result, string successMessage, string action)
    {
        if (result.IsSuccess)
        {
            TempData["VocabSuccess"] = successMessage;
            return;
        }

        TempData["VocabError"] = result.StatusCode switch
        {
            401 or 403 => "You do not have permission to perform this action (super admin required).",
            404 => "Word not found.",
            _ => result.Message ?? $"Unable to {action} the word.",
        };
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
