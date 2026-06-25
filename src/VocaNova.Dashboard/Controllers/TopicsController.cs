using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Topics;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F061 — Topic Management. List + inline CRUD; delete guarded khi topic còn từ active (API trả 409).
public sealed class TopicsController : Controller
{
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
        CancellationToken cancellationToken = default)
    {
        var topics = await _apiClient.GetAdminTopicsAsync(
            string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            string.IsNullOrWhiteSpace(status) ? null : status,
            includeDeleted,
            cancellationToken);

        var model = new TopicListViewModel
        {
            Items = topics,
            Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            Status = string.IsNullOrWhiteSpace(status) ? null : status,
            IncludeDeleted = includeDeleted,
        };

        return View(model);
    }

    [HttpPost("/topics/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] TopicInput input, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.CreateTopicAsync(input, cancellationToken);
        SetFeedback(result, "Topic created.");
        return RedirectBack(returnUrl);
    }

    [HttpPost("/topics/{id:uint}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(uint id, [FromForm] TopicInput input, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _apiClient.UpdateTopicAsync(id, input, cancellationToken);
        SetFeedback(result, "Topic updated.");
        return RedirectBack(returnUrl);
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
