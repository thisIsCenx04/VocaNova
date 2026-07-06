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

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<int>.Unauthorized("Unauthorized."));
        }

        var result = await _notificationService.UnreadCountAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(new { count = result.Value }, "Unread count loaded successfully.");
    }

    [HttpPatch("{id:uint}/read")]
    public async Task<IActionResult> MarkRead(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _notificationService.MarkReadAsync(userId, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "Notification marked as read.");
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<int>.Unauthorized("Unauthorized."));
        }

        var result = await _notificationService.MarkAllReadAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(new { updated = result.Value }, "All notifications marked as read.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
