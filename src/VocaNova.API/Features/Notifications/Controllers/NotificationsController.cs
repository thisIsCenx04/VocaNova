using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Notifications.DTOs;
using VocaNova.API.Features.Notifications.Services;

namespace VocaNova.API.Features.Notifications.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // Notifications are derived on read; read/unread state and "mark read" are handled on the
    // client (per-device), so there is no write endpoint here.
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] NotificationListQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<PagedResult<NotificationDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _notificationService.ListAsync(userId, query, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return Ok(ApiResponseFormatter.Paged(result.Value!, "Notifications loaded successfully."));
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
