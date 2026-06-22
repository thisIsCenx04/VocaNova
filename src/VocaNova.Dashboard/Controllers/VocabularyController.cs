using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.Net.Http.Headers;
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

    [HttpGet("/vocabulary/{id:long}")]
    public async Task<IActionResult> Detail(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.GetAsync<WordDetailDto>($"api/words/{id}", cancellationToken);
        var model = new VocabularyDetailViewModel
        {
            Word = result.IsSuccess ? result.Data : null,
            Loaded = result.IsSuccess && result.Data is not null,
            ErrorMessage = result.Message,
            SenseDeleteAvailable = false,
            ExampleMutationAvailable = false,
        };

        return View(model);
    }

    [HttpGet("/vocabulary/import")]
    public IActionResult Import()
    {
        return View(new VocabularyImportViewModel());
    }

    [HttpPost("/vocabulary/import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsv(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = _l["Import.Validation.Required"].Value });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = _l["Import.Validation.CsvOnly"].Value });
        }

        if (file.Length > VocabularyImportViewModel.MaxFileBytes)
        {
            return BadRequest(new { success = false, message = _l["Import.Validation.TooLarge"].Value });
        }

        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
        {
            fileContent.Headers.ContentType = contentType;
        }
        content.Add(fileContent, "File", file.FileName);

        var result = await _api.PostFormAsync<BulkImportResultDto>("api/admin/words/import", content, cancellationToken);
        var message = result.IsSuccess
            ? _l["Import.Toast.Completed"].Value
            : (string.IsNullOrWhiteSpace(result.Message) ? _l["Toast.ActionFailed"].Value : result.Message);

        return StatusCode(result.IsSuccess ? StatusCodes.Status200OK : NormalizeErrorStatus(result.StatusCode), new
        {
            success = result.IsSuccess,
            message,
            data = result.Data,
            errors = result.Errors,
        });
    }

    [HttpPost("/vocabulary/{id:long}/senses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSense(uint id, [FromForm] SenseInputModel input, CancellationToken cancellationToken)
    {
        var validation = ValidateSense(input);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var result = await _api.PostAsync<WordSenseDto>($"api/admin/words/{id}/senses", input, cancellationToken);
        return MutationResult(result, "Toast.SenseCreated", id);
    }

    [HttpPost("/vocabulary/{id:long}/senses/{senseId:long}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSense(uint id, uint senseId, [FromForm] SenseInputModel input, CancellationToken cancellationToken)
    {
        var validation = ValidateSense(input);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var result = await _api.PutAsync<WordSenseDto>($"api/admin/words/{id}/senses/{senseId}", input, cancellationToken);
        return MutationResult(result, "Toast.SenseUpdated", id);
    }

    // Kept ready for the existing API contract. The UI remains disabled until the API
    // service/database support sense soft-delete (tracked in DASHBOARD_WORKLOG.md).
    [HttpPost("/vocabulary/{id:long}/senses/{senseId:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSense(uint id, uint senseId, CancellationToken cancellationToken)
    {
        var result = await _api.DeleteAsync<object>($"api/admin/words/{id}/senses/{senseId}", cancellationToken);
        return MutationResult(result, "Toast.SenseDeleted", id);
    }

    [HttpPost("/vocabulary/{id:long}/senses/{senseId:long}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreSense(uint id, uint senseId, CancellationToken cancellationToken)
    {
        var result = await _api.PatchAsync<object>($"api/admin/words/{id}/senses/{senseId}/restore", null, cancellationToken);
        return MutationResult(result, "Toast.SenseRestored", id);
    }

    [HttpPost("/vocabulary/{id:long}/audio")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAudio(uint id, IFormFile? file, string? accent, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = _l["Validation.AudioRequired"].Value });
        }

        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        }
        content.Add(fileContent, "File", file.FileName);
        content.Add(new StringContent(accent ?? string.Empty), "Accent");

        var result = await _api.PostFormAsync<WordAudioDto>($"api/admin/words/{id}/audio", content, cancellationToken);
        return MutationResult(result, "Toast.AudioUploaded", id);
    }

    [HttpPost("/vocabulary/{id:long}/audio/{audioId:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAudio(uint id, uint audioId, CancellationToken cancellationToken)
    {
        var result = await _api.DeleteAsync<object>($"api/admin/words/{id}/audio/{audioId}", cancellationToken);
        SetToast(result.IsSuccess, "Toast.AudioDeleted", result.Message);
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/vocabulary/{id:long}/image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(uint id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = _l["Validation.ImageRequired"].Value });
        }

        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        }
        content.Add(fileContent, "File", file.FileName);

        var result = await _api.PostFormAsync<WordDetailDto>($"api/admin/words/{id}/image", content, cancellationToken);
        return MutationResult(result, "Toast.ImageUploaded", id);
    }

    [HttpPost("/vocabulary/{id:long}/image-url")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImageUrl(uint id, string? imageUrl, CancellationToken cancellationToken)
    {
        var result = await _api.PutAsync<WordDetailDto>($"api/admin/words/{id}/image", new { imageUrl }, cancellationToken);
        return MutationResult(result, "Toast.ImageUpdated", id);
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

    private IActionResult MutationResult<T>(ApiResult<T> result, string successKey, uint wordId)
    {
        var message = result.IsSuccess
            ? _l[successKey].Value
            : (string.IsNullOrWhiteSpace(result.Message) ? _l["Toast.ActionFailed"].Value : result.Message);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return StatusCode(result.IsSuccess ? StatusCodes.Status200OK : NormalizeErrorStatus(result.StatusCode), new
            {
                success = result.IsSuccess,
                message,
                errors = result.Errors,
            });
        }

        SetToast(result.IsSuccess, successKey, result.Message);
        return RedirectToAction(nameof(Detail), new { id = wordId });
    }

    private string? ValidateSense(SenseInputModel input)
    {
        if (input.SenseOrder < 1)
        {
            return _l["Validation.SenseOrder"].Value;
        }
        if (string.IsNullOrWhiteSpace(input.WordClass))
        {
            return _l["Validation.WordClassRequired"].Value;
        }
        if (string.IsNullOrWhiteSpace(input.EnglishDefinition))
        {
            return _l["Validation.EnglishDefinitionRequired"].Value;
        }

        return null;
    }

    private static int NormalizeErrorStatus(int statusCode)
        => statusCode is >= 400 and <= 599 ? statusCode : StatusCodes.Status400BadRequest;

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
