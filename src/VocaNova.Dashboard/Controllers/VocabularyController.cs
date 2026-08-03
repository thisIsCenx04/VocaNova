using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Vocabulary;
using VocaNova.Dashboard.Services.Api;
using VocaNova.Dashboard.Services.Localization;

namespace VocaNova.Dashboard.Controllers;

// F057 — Vocabulary List & Filter. Phân trang server-side; xóa/khôi phục proxy qua API rồi redirect giữ nguyên bộ lọc.
public sealed class VocabularyController : Controller
{
    private const int DefaultPageSize = 10;
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100];

    // Các cột cho phép sắp xếp — khớp AdminSortColumns của API; giá trị khác bị bỏ qua.
    private static readonly HashSet<string> SortableColumns =
        new(StringComparer.Ordinal) { "word", "type", "cefr", "phonetic", "status" };

    private readonly IVocaNovaApiClient _apiClient;
    private readonly ITranslator _translator;

    public VocabularyController(IVocaNovaApiClient apiClient, ITranslator translator)
    {
        _apiClient = apiClient;
        _translator = translator;
    }

    [HttpGet("/vocabulary")]
    public async Task<IActionResult> Index(
        string? q,
        string? cefr,
        uint? topicId,
        string? status,
        string? wordType,
        string? sortBy = null,
        string? sortDirection = null,
        bool includeDeleted = false,
        int page = 1,
        int limit = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (!AllowedPageSizes.Contains(limit))
        {
            limit = DefaultPageSize;
        }

        // Chỉ chấp nhận cột hợp lệ và hướng asc/desc; ngược lại bỏ sort (về mặc định).
        var normalizedSort = sortBy?.Trim().ToLowerInvariant();
        if (normalizedSort is null || !SortableColumns.Contains(normalizedSort))
        {
            normalizedSort = null;
        }

        var normalizedDir = sortDirection?.Trim().ToLowerInvariant() == "desc" ? "desc" : "asc";
        if (normalizedSort is null)
        {
            normalizedDir = "asc";
        }

        var filter = new WordListFilter(
            Q: string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            Cefr: string.IsNullOrWhiteSpace(cefr) ? null : cefr,
            TopicId: topicId,
            Status: string.IsNullOrWhiteSpace(status) ? null : status,
            IncludeDeleted: includeDeleted,
            Page: page,
            Limit: limit,
            WordType: string.IsNullOrWhiteSpace(wordType) ? null : wordType,
            SortBy: normalizedSort,
            SortDirection: normalizedSort is null ? null : normalizedDir);

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
            WordType = filter.WordType,
            IncludeDeleted = includeDeleted,
            SortBy = filter.SortBy,
            SortDirection = filter.SortDirection,
            Page = words.Page,
            Limit = words.Limit,
            TotalItems = words.TotalItems,
            TotalPages = words.TotalPages,
        };

        return View(model);
    }

    // Create Vocabulary Metadata (Figma): thông tin cơ bản + phiên âm + một/nhiều nghĩa (Word type + EN/VI).
    // Ví dụ (examples) hiển thị theo thiết kế nhưng API chưa hỗ trợ CRUD nên không được lưu.
    [HttpGet("/vocabulary/create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("/vocabulary/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] string? word,
        [FromForm] string? cefr,
        [FromForm] string? phoneticUk,
        [FromForm] string[]? senseWordClass,
        [FromForm] string[]? meaningEn,
        [FromForm] string[]? meaningVi,
        [FromForm] string[]? exampleEn,
        [FromForm] string[]? exampleVi,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            TempData["VocabError"] = "Word is required.";
            return RedirectToAction(nameof(Create));
        }

        // 1) Tạo word (lấy word_id để gắn nghĩa).
        var (result, newId) = await _apiClient.CreateWordWithIdAsync(
            new WordInput(
                word.Trim(),
                string.IsNullOrWhiteSpace(cefr) ? null : cefr,
                string.IsNullOrWhiteSpace(phoneticUk) ? null : phoneticUk.Trim(),
                null,
                false),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["VocabError"] = result.StatusCode switch
            {
                409 => "That word already exists.",
                401 or 403 => "You do not have permission to create vocabulary.",
                400 => result.Message ?? "Invalid vocabulary data.",
                _ => result.Message ?? "Unable to create vocabulary.",
            };
            return RedirectToAction(nameof(Create));
        }

        // 2) Tạo các nghĩa (chỉ những block có định nghĩa EN hoặc VI).
        var senseErrors = new List<string>();
        if (newId is { } wordId)
        {
            var classes = senseWordClass ?? Array.Empty<string>();
            var ens = meaningEn ?? Array.Empty<string>();
            var vis = meaningVi ?? Array.Empty<string>();
            var exEns = exampleEn ?? Array.Empty<string>();
            var exVis = exampleVi ?? Array.Empty<string>();
            var blockCount = Math.Max(ens.Length, vis.Length);
            var order = 1;

            for (var i = 0; i < blockCount; i++)
            {
                var en = i < ens.Length ? ens[i] : null;
                var vi = i < vis.Length ? vis[i] : null;
                if (string.IsNullOrWhiteSpace(en) && string.IsNullOrWhiteSpace(vi))
                {
                    continue;
                }

                var wordClass = i < classes.Length && !string.IsNullOrWhiteSpace(classes[i])
                    ? classes[i].Trim().ToLowerInvariant()
                    : null;

                // Mỗi block nghĩa kèm 1 ví dụ (nếu có) — căn theo cùng chỉ số block.
                var exEn = i < exEns.Length ? exEns[i] : null;
                var exVi = i < exVis.Length ? exVis[i] : null;
                List<SenseExampleInput>? examples = null;
                if (!string.IsNullOrWhiteSpace(exEn))
                {
                    examples = new List<SenseExampleInput>
                    {
                        new(null, exEn.Trim(), string.IsNullOrWhiteSpace(exVi) ? null : exVi.Trim()),
                    };
                }

                var senseResult = await _apiClient.CreateSenseAsync(
                    wordId,
                    new SenseInput(order, wordClass, string.IsNullOrWhiteSpace(en) ? null : en.Trim(),
                        string.IsNullOrWhiteSpace(vi) ? null : vi.Trim(), examples),
                    cancellationToken);

                if (senseResult.IsSuccess)
                {
                    order++;
                }
                else
                {
                    senseErrors.Add(senseResult.Message ?? $"meaning #{i + 1}");
                }
            }
        }

        TempData["VocabSuccess"] = senseErrors.Count == 0
            ? $"Vocabulary \"{word.Trim()}\" created."
            : $"Vocabulary \"{word.Trim()}\" created, but some meanings failed: {string.Join("; ", senseErrors)}";
        return RedirectToAction(nameof(Index));
    }

    // Update Vocabulary Metadata — màn Edit theo Figma: thông tin cơ bản + định nghĩa/ví dụ + trạng thái lưu trữ.
    [HttpGet("/vocabulary/{id:uint}/edit")]
    public async Task<IActionResult> Edit(uint id, CancellationToken cancellationToken)
    {
        var detail = await _apiClient.GetWordDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    [HttpPost("/vocabulary/{id:uint}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        uint id,
        [FromForm] string? word,
        [FromForm] string? wordType,
        [FromForm] string? cefr,
        [FromForm] string? phoneticUk,
        [FromForm] string? phoneticUs,
        [FromForm] string? imageUrl,
        [FromForm] bool isPhrase,
        [FromForm] bool isActive,
        [FromForm] bool wasActive,
        [FromForm] uint[]? senseId,
        [FromForm] int[]? senseOrder,
        [FromForm] string[]? senseWordClass,
        [FromForm] string[]? englishDefinition,
        [FromForm] string[]? vietnameseMeaning,
        [FromForm] int[]? exampleSenseIdx,
        [FromForm] uint[]? exampleId,
        [FromForm] string[]? exampleEn,
        [FromForm] string[]? exampleVi,
        [FromForm] string? newEnglishDefinition,
        [FromForm] string? newVietnameseMeaning,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            TempData["VocabError"] = "Word is required.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var normalizedType = string.IsNullOrWhiteSpace(wordType) ? null : wordType.Trim().ToLowerInvariant();
        var errors = new List<string>();

        // 1) Thông tin cơ bản — PUT metadata (giữ nguyên phonetic US + is_phrase ẩn).
        var metaResult = await _apiClient.UpdateWordAsync(
            id,
            new WordInput(
                word.Trim(),
                string.IsNullOrWhiteSpace(cefr) ? null : cefr,
                string.IsNullOrWhiteSpace(phoneticUk) ? null : phoneticUk.Trim(),
                string.IsNullOrWhiteSpace(phoneticUs) ? null : phoneticUs.Trim(),
                isPhrase,
                string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()),
            cancellationToken);
        if (!metaResult.IsSuccess)
        {
            TempData["VocabError"] = metaResult.StatusCode switch
            {
                409 => "That word already exists.",
                401 or 403 => "You do not have permission to edit vocabulary.",
                _ => metaResult.Message ?? "Unable to update the word.",
            };
            return RedirectToAction(nameof(Edit), new { id });
        }

        // Gom ví dụ theo chỉ số block của sense (exampleSenseIdx căn vị trí với exampleId/En/Vi).
        var exSenseIdx = exampleSenseIdx ?? Array.Empty<int>();
        var exIds = exampleId ?? Array.Empty<uint>();
        var exEns = exampleEn ?? Array.Empty<string>();
        var exVis = exampleVi ?? Array.Empty<string>();

        List<SenseExampleInput> ExamplesForBlock(int blockIdx)
        {
            var list = new List<SenseExampleInput>();
            for (var k = 0; k < exSenseIdx.Length; k++)
            {
                if (exSenseIdx[k] != blockIdx)
                {
                    continue;
                }

                var een = k < exEns.Length ? exEns[k] : null;
                if (string.IsNullOrWhiteSpace(een))
                {
                    continue;
                }

                var eid = k < exIds.Length && exIds[k] > 0 ? (uint?)exIds[k] : null;
                var evi = k < exVis.Length ? exVis[k] : null;
                list.Add(new SenseExampleInput(eid, een.Trim(), string.IsNullOrWhiteSpace(evi) ? null : evi.Trim()));
            }

            return list;
        }

        // 2) Định nghĩa & ví dụ — cập nhật từng sense (sense đầu nhận Word Type của màn).
        var ids = senseId ?? Array.Empty<uint>();
        for (var i = 0; i < ids.Length; i++)
        {
            var en = englishDefinition is not null && i < englishDefinition.Length ? englishDefinition[i] : null;
            if (string.IsNullOrWhiteSpace(en))
            {
                continue;
            }

            var existingClass = senseWordClass is not null && i < senseWordClass.Length ? senseWordClass[i] : null;
            var wordClass = i == 0 && normalizedType is not null ? normalizedType : existingClass;
            var order = senseOrder is not null && i < senseOrder.Length ? senseOrder[i] : i + 1;
            var vi = vietnameseMeaning is not null && i < vietnameseMeaning.Length ? vietnameseMeaning[i] : null;

            var senseResult = await _apiClient.UpdateSenseAsync(
                id,
                ids[i],
                new SenseInput(order, wordClass, en.Trim(), string.IsNullOrWhiteSpace(vi) ? null : vi.Trim(),
                    ExamplesForBlock(i)),
                cancellationToken);
            if (!senseResult.IsSuccess)
            {
                errors.Add($"sense #{order}");
            }
        }

        // 3) Định nghĩa mới (nếu nhập) — tạo sense kế tiếp.
        if (!string.IsNullOrWhiteSpace(newEnglishDefinition))
        {
            var nextOrder = (senseOrder is { Length: > 0 } ? senseOrder.Max() : ids.Length) + 1;
            var addResult = await _apiClient.CreateSenseAsync(
                id,
                new SenseInput(
                    nextOrder,
                    normalizedType ?? "other",
                    newEnglishDefinition.Trim(),
                    string.IsNullOrWhiteSpace(newVietnameseMeaning) ? null : newVietnameseMeaning.Trim()),
                cancellationToken);
            if (!addResult.IsSuccess)
            {
                errors.Add("new meaning");
            }
        }

        // 4) Trạng thái lưu trữ — toggle Active = restore / deactivate (xóa mềm).
        if (wasActive && !isActive)
        {
            var off = await _apiClient.DeleteWordAsync(id, cancellationToken);
            if (!off.IsSuccess)
            {
                errors.Add("status");
            }
        }
        else if (!wasActive && isActive)
        {
            var on = await _apiClient.RestoreWordAsync(id, cancellationToken);
            if (!on.IsSuccess)
            {
                errors.Add("status");
            }
        }

        if (errors.Count > 0)
        {
            TempData["VocabError"] = $"Saved core info, but these parts failed: {string.Join(", ", errors)}.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["VocabSuccess"] = $"\"{word.Trim()}\" updated.";
        return RedirectToAction(nameof(Index));
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
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Json(new { success = false, message = _translator["Please choose an audio file."] });
        }

        await using var stream = file.OpenReadStream();
        var result = await _apiClient.UploadAudioAsync(
            id,
            new AudioUpload("us", stream, file.FileName, file.ContentType),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return JsonResultFor(result, "Audio uploaded.");
        }

        var detail = await _apiClient.GetWordDetailAsync(id, cancellationToken);
        var audioId = detail?.Audio
            .Where(audio => string.Equals(audio.Accent, "us", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(audio => audio.AudioId)
            .Select(audio => (uint?)audio.AudioId)
            .FirstOrDefault();
        return Json(new { success = true, message = _translator["Audio uploaded."], audioId });
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
            return Json(new { success = false, message = _translator["Please choose an image file."] });
        }

        await using var stream = file.OpenReadStream();
        var result = await _apiClient.UploadImageAsync(
            id,
            new ImageUpload(stream, file.FileName, file.ContentType),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return JsonResultFor(result, "Image uploaded.");
        }

        var detail = await _apiClient.GetWordDetailAsync(id, cancellationToken);
        return Json(new { success = true, message = _translator["Image uploaded."], imageUrl = detail?.ImageUrl });
    }

    [HttpPost("/vocabulary/{id:uint}/image/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(uint id, CancellationToken cancellationToken)
    {
        var result = await _apiClient.UpdateImageUrlAsync(id, null, cancellationToken);
        return JsonResultFor(result, "Image removed.");
    }

    // ---- F059: CSV import ----

    [HttpGet("/vocabulary/import")]
    public IActionResult Import()
    {
        return View();
    }

    [HttpPost("/vocabulary/import")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Json(new { success = false, message = _translator["Please choose a CSV file."] });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = _translator["File extension must be .csv."] });
        }

        await using var stream = file.OpenReadStream();
        var result = await _apiClient.ImportWordsAsync(
            new FileUpload(stream, file.FileName, string.IsNullOrWhiteSpace(file.ContentType) ? "text/csv" : file.ContentType),
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            var message = result.StatusCode switch
            {
                401 or 403 => "You do not have permission to import words.",
                400 => result.Message ?? "The CSV file is invalid.",
                _ => result.Message ?? "Import failed.",
            };
            return Json(new { success = false, message = _translator[message] });
        }

        return Json(new { success = true, result = result.Data });
    }

    [HttpGet("/vocabulary/import/template")]
    public IActionResult ImportTemplate()
    {
        const string csv =
            "word,cefr_level,phonetic_uk,phonetic_us,word_class,english_definition,vietnamese_meaning,is_phrase,topic_names,example_en,example_vi,image_url\n" +
            "run,A1,/run-uk/,/run-us/,verb,to move fast on foot,chay,false,Movement|Sports,I run every morning.,Toi chay moi sang.,\n" +
            "happy,A2,/happy-uk/,/happy-us/,adjective,feeling pleasure,vui ve,false,Emotions,She looks happy today.,Hom nay co ay trong vui.,\n";
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv))
            .ToArray();
        return File(bytes, "text/csv", "vocabulary-import-template.csv");
    }

    // Chuẩn hóa kết quả AJAX: { success, message }.
    private IActionResult JsonResultFor(ApiActionResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            return Json(new { success = true, message = _translator[successMessage] });
        }

        var message = result.StatusCode switch
        {
            401 or 403 => "You do not have permission to perform this action.",
            404 => "Resource not found.",
            _ => result.Message ?? "The request could not be completed.",
        };
        return Json(new { success = false, message = _translator[message] });
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
