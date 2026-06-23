using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VocaNova.Dashboard.Models.Api.Dictionary;
using VocaNova.Dashboard.Models.Topics;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class TopicsController : Controller
{
    private readonly IVocaNovaApiClient _api;
    private readonly IStringLocalizer<SharedResource> _l;

    public TopicsController(IVocaNovaApiClient api, IStringLocalizer<SharedResource> l)
    {
        _api = api;
        _l = l;
    }

    [HttpGet("/topics")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var topics = await _api.GetAsync<List<TopicSummaryDto>>("api/topics", cancellationToken);

        return View(new TopicListViewModel
        {
            Topics = topics.IsSuccess && topics.Data is not null ? topics.Data : new List<TopicSummaryDto>(),
            Loaded = topics.IsSuccess,
            // G6: chưa có admin topic list includeDeleted → chưa liệt kê được topic đã xóa để restore.
            RestoreAvailable = false,
        });
    }

    [HttpGet("/topics/create")]
    public IActionResult CreateForm() => PartialView("_TopicForm", new TopicFormViewModel());

    [HttpPost("/topics/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] TopicFormViewModel input, CancellationToken cancellationToken)
    {
        var error = Validate(input);
        if (error is not null)
        {
            input.Error = error;
            return PartialView("_TopicForm", input);
        }

        var result = await _api.PostAsync<TopicSummaryDto>("api/admin/topics", ToRequest(input), cancellationToken);
        if (!result.IsSuccess)
        {
            input.Error = ErrorMessage(result.Message);
            return PartialView("_TopicForm", input);
        }

        return Json(new { message = _l["Toast.TopicCreated"].Value });
    }

    [HttpGet("/topics/{id:long}/edit")]
    public async Task<IActionResult> EditForm(uint id, CancellationToken cancellationToken)
    {
        var topics = await _api.GetAsync<List<TopicSummaryDto>>("api/topics", cancellationToken);
        var topic = topics.Data?.FirstOrDefault(t => t.TopicId == id);
        if (topic is null)
        {
            return PartialView("_TopicForm", new TopicFormViewModel { TopicId = id, Error = _l["Topic.NotFound"].Value });
        }

        return PartialView("_TopicForm", new TopicFormViewModel
        {
            TopicId = topic.TopicId,
            TopicName = topic.Name,
            TopicNameVi = topic.NameVi,
            Icon = topic.Icon,
        });
    }

    [HttpPost("/topics/{id:long}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(uint id, [FromForm] TopicFormViewModel input, CancellationToken cancellationToken)
    {
        input.TopicId = id;
        var error = Validate(input);
        if (error is not null)
        {
            input.Error = error;
            return PartialView("_TopicForm", input);
        }

        var result = await _api.PutAsync<TopicSummaryDto>($"api/admin/topics/{id}", ToRequest(input), cancellationToken);
        if (!result.IsSuccess)
        {
            input.Error = ErrorMessage(result.Message);
            return PartialView("_TopicForm", input);
        }

        return Json(new { message = _l["Toast.TopicUpdated"].Value });
    }

    [HttpPost("/topics/{id:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        // API trả 409 nếu topic còn từ active → message hiển thị qua toast.
        var result = await _api.DeleteAsync<object>($"api/admin/topics/{id}", cancellationToken);
        SetToast(result.IsSuccess, "Toast.TopicDeleted", result.Message);
        return RedirectToAction(nameof(Index));
    }

    // Khung sẵn theo contract API (PATCH restore đã có). UI chưa bật được vì G6: dashboard
    // chưa liệt kê topic đã xóa. Khi có admin topic list includeDeleted → bật nút Restore.
    [HttpPost("/topics/{id:long}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(uint id, CancellationToken cancellationToken)
    {
        var result = await _api.PatchAsync<object>($"api/admin/topics/{id}/restore", body: null, cancellationToken);
        SetToast(result.IsSuccess, "Toast.TopicRestored", result.Message);
        return RedirectToAction(nameof(Index));
    }

    private static object ToRequest(TopicFormViewModel m) => new
    {
        topic_name = m.TopicName?.Trim(),
        topic_name_vi = string.IsNullOrWhiteSpace(m.TopicNameVi) ? null : m.TopicNameVi.Trim(),
        icon = string.IsNullOrWhiteSpace(m.Icon) ? null : m.Icon.Trim(),
    };

    // Mirror backend validators: name required ≤50, name_vi ≤50, icon ≤20.
    private string? Validate(TopicFormViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.TopicName))
        {
            return _l["Topic.Validation.NameRequired"].Value;
        }
        if (m.TopicName.Trim().Length > 50)
        {
            return _l["Topic.Validation.NameMax"].Value;
        }
        if (!string.IsNullOrWhiteSpace(m.TopicNameVi) && m.TopicNameVi.Trim().Length > 50)
        {
            return _l["Topic.Validation.NameViMax"].Value;
        }
        if (!string.IsNullOrWhiteSpace(m.Icon) && m.Icon.Trim().Length > 20)
        {
            return _l["Topic.Validation.IconMax"].Value;
        }

        return null;
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
