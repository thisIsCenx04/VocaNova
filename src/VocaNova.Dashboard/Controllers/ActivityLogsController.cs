using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using VocaNova.Dashboard.Models.Activity;
using VocaNova.Dashboard.Models.Api.Audit;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class ActivityLogsController : Controller
{
    private readonly IVocaNovaApiClient _api;

    public ActivityLogsController(IVocaNovaApiClient api)
    {
        _api = api;
    }

    [HttpGet("/activity-logs")]
    public async Task<IActionResult> Index([FromQuery] ActivityLogQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            query.Page = 1;
        }

        var logs = await _api.GetPagedAsync<AuditLogDto>(BuildUrl(query), cancellationToken);

        return View(new ActivityLogViewModel
        {
            Query = query,
            Logs = logs.IsSuccess ? logs.Items : Array.Empty<AuditLogDto>(),
            Loaded = logs.IsSuccess,
            Pagination = logs.Pagination,
        });
    }

    private static string BuildUrl(ActivityLogQuery q)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["page"] = q.Page.ToString(),
            ["limit"] = q.Limit.ToString(),
        };
        if (q.UserId is > 0)
        {
            parameters["user_id"] = q.UserId.Value.ToString();
        }
        if (!string.IsNullOrWhiteSpace(q.Entity))
        {
            parameters["entity"] = q.Entity.Trim();
        }

        return QueryHelpers.AddQueryString("api/admin/audit-logs", parameters);
    }
}
