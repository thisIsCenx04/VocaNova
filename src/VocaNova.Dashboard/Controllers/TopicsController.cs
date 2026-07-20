using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Topics;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F061 — Topic Management. List + inline CRUD; delete guarded khi topic còn từ active (API trả 409).
public sealed class TopicsController : Controller
{
    private const int DefaultPageSize = 10;
    private static readonly int[] AllowedPageSizes = [10, 20, 30, 40, 50];
    private readonly IVocaNovaApiClient _apiClient;

    public TopicsController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/topics")]
    public async Task<IActionResult> Index(
        string? q,
        string? status,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : DefaultPageSize;
        var topics = await _apiClient.GetAdminTopicsAsync(
            string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            string.IsNullOrWhiteSpace(status) ? null : status,
            includeDeleted,
            cancellationToken);

        var totalItems = topics.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Min(page, totalPages);
        var model = new TopicListViewModel
        {
            Items = topics.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
            Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            Status = string.IsNullOrWhiteSpace(status) ? null : status,
            IncludeDeleted = includeDeleted,
            Page = page,
            TotalItems = totalItems,
            TotalPages = totalPages,
            PageSize = pageSize,
        };

        return View(model);
    }

    [HttpGet("/topics/{id:uint}")]
    public async Task<IActionResult> Detail(
        uint id, string? q, string? cefr, string? status, string? wordType,
        bool includeDeleted = false, int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var topics = await _apiClient.GetAdminTopicsAsync(null, null, true, cancellationToken);
        var topic = topics.SingleOrDefault(item => item.TopicId == id);
        if (topic is null)
        {
            return NotFound();
        }

        var filter = new WordListFilter(
            string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            string.IsNullOrWhiteSpace(cefr) ? null : cefr,
            id,
            string.IsNullOrWhiteSpace(status) ? null : status,
            includeDeleted,
            page,
            DefaultPageSize,
            string.IsNullOrWhiteSpace(wordType) ? null : wordType);
        var words = await _apiClient.GetWordsAsync(filter, cancellationToken);

        return View(new TopicDetailViewModel
        {
            Topic = topic, Items = words.Items, Q = filter.Q, Cefr = filter.Cefr,
            Status = filter.Status, WordType = filter.WordType, IncludeDeleted = includeDeleted,
            Page = words.Page, Limit = words.Limit, TotalItems = words.TotalItems, TotalPages = words.TotalPages,
        });
    }

    [HttpGet("/topics/create")]
    public IActionResult CreatePage()
    {
        return View("Create", new TopicCreateViewModel());
    }

    [HttpGet("/topics/{id:uint}/edit")]
    public async Task<IActionResult> EditPage(uint id, CancellationToken cancellationToken)
    {
        var topics = await _apiClient.GetAdminTopicsAsync(null, null, false, cancellationToken);
        var topic = topics.SingleOrDefault(item => item.TopicId == id);
        if (topic is null) return NotFound();

        var words = await _apiClient.GetWordsAsync(
            new WordListFilter(null, null, id, null, true, 1, 100),
            cancellationToken);

        return View("Edit", new TopicEditViewModel
        {
            TopicId = topic.TopicId,
            Icon = topic.Icon,
            TopicName = topic.TopicName,
            TopicNameVi = topic.TopicNameVi,
            Keywords = words.Items.Select(word => word.Word).ToList(),
            WordIds = words.Items.Select(word => word.WordId).ToList(),
        });
    }

    [HttpGet("/topics/word-suggestions")]
    public async Task<IActionResult> WordSuggestions(string? q, CancellationToken cancellationToken)
    {
        var term = q?.Trim();
        if (string.IsNullOrEmpty(term))
        {
            return Json(Array.Empty<object>());
        }

        var words = await _apiClient.GetWordsAsync(
            new WordListFilter(term, null, null, "active", false, 1, 8),
            cancellationToken);

        return Json(words.Items.Select(word => new
        {
            word.WordId,
            word.Word,
            word.PrimaryMeaning,
        }));
    }

    [HttpPost("/topics/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] TopicCreateViewModel model, CancellationToken cancellationToken)
    {
        model.Keywords = model.Keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .ToList();

        var duplicateKeyword = model.Keywords
            .GroupBy(keyword => keyword, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateKeyword is not null)
        {
            ModelState.AddModelError(nameof(model.Keywords), $"The vocabulary '{duplicateKeyword}' has already been added.");
        }

        if (model.WordIds.Count != model.Keywords.Count
            || model.WordIds.Any(id => id == 0)
            || model.WordIds.Count != model.WordIds.Distinct().Count())
        {
            ModelState.AddModelError(nameof(model.Keywords), "Please select unique vocabulary words from the suggestions.");
        }

        if (!ModelState.IsValid)
        {
            return View("Create", model);
        }

        model.TopicName = model.TopicName?.Trim();
        model.TopicNameVi = string.IsNullOrWhiteSpace(model.TopicNameVi) ? null : model.TopicNameVi.Trim();
        model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim();

        var existingTopics = await _apiClient.GetAdminTopicsAsync(null, null, false, cancellationToken);
        if (existingTopics.Any(topic => string.Equals(
            topic.TopicName.Trim(), model.TopicName, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.TopicName), "Topic already exists.");
            return View("Create", model);
        }
        if (model.TopicNameVi is not null && existingTopics.Any(topic => string.Equals(
            topic.TopicNameVi?.Trim(), model.TopicNameVi, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.TopicNameVi), "Vietnamese topic name already exists.");
            return View("Create", model);
        }

        var input = new TopicInput(model.TopicName, model.TopicNameVi, model.Icon, model.WordIds);
        var result = await _apiClient.CreateTopicAsync(input, cancellationToken);
        SetFeedback(result, "Topic created.");
        if (!result.IsSuccess)
        {
            return View("Create", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/topics/{id:uint}/words")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWords(uint id, [FromForm] List<uint> wordIds, CancellationToken cancellationToken)
    {
        var result = await _apiClient.AddWordsToTopicAsync(id, wordIds, cancellationToken);
        SetFeedback(result, result.IsSuccess ? "Vocabulary added to topic." : "");
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/topics/{id:uint}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(uint id, [FromForm] TopicEditViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.TopicId) return BadRequest();
        model.Keywords = model.Keywords.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList();
        if (model.WordIds.Count != model.Keywords.Count
            || model.WordIds.Any(wordId => wordId == 0)
            || model.WordIds.Count != model.WordIds.Distinct().Count())
        {
            ModelState.AddModelError(nameof(model.Keywords), "Please select unique vocabulary words from the suggestions.");
        }
        if (!ModelState.IsValid) return View("Edit", model);

        model.TopicName = model.TopicName?.Trim();
        model.TopicNameVi = string.IsNullOrWhiteSpace(model.TopicNameVi) ? null : model.TopicNameVi.Trim();
        model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim();
        var existingTopics = await _apiClient.GetAdminTopicsAsync(null, null, false, cancellationToken);
        if (existingTopics.Any(topic => topic.TopicId != id && string.Equals(
            topic.TopicName.Trim(), model.TopicName, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.TopicName), "Topic already exists.");
        }
        if (model.TopicNameVi is not null && existingTopics.Any(topic => topic.TopicId != id && string.Equals(
            topic.TopicNameVi?.Trim(), model.TopicNameVi, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(model.TopicNameVi), "Vietnamese topic name already exists.");
        }
        if (!ModelState.IsValid) return View("Edit", model);

        var input = new TopicInput(model.TopicName, model.TopicNameVi, model.Icon, model.WordIds);
        var result = await _apiClient.UpdateTopicAsync(id, input, cancellationToken);
        SetFeedback(result, "Topic updated.");
        if (!result.IsSuccess) return View("Edit", model);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/topics/{id:uint}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.DeleteTopicAsync(id, cancellationToken);
        SetFeedback(result, "Topic deleted.");
        return RedirectBack(returnUrl);
    }

    [HttpPost("/topics/{id:uint}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(uint id, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.RestoreTopicAsync(id, cancellationToken);
        SetFeedback(result, "Topic restored.");
        return RedirectBack(returnUrl);
    }

    private void SetFeedback(ApiActionResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            TempData["TopicSuccess"] = successMessage;
            return;
        }

        TempData["TopicError"] = result.StatusCode switch
        {
            401 or 403 => "You do not have permission to perform this action.",
            404 => "Topic not found.",
            409 => result.Message ?? "Cannot delete: this topic still has active words.",
            400 => result.Message ?? "Invalid topic data.",
            _ => result.Message ?? "The request could not be completed.",
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
