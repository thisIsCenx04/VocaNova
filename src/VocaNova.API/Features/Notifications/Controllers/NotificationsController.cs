using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Features.Notifications.BLL.Services;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Notifications.Contracts.Requests;
using VocaNova.API.Features.Notifications.Mappings;
using VocaNova.API.Features.Notifications.BLL.Services.IServices;

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
        [FromQuery] NotificationListRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponseFormatter.Error(
                "Unauthorized.",
                new[] { "Unauthorized." }));
        }

        var result = await _notificationService.ListAsync(
            userId,
            request.ToBusinessQuery(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponseFormatter.Error(
                result.Error!,
                new[] { result.Error! }));
        }

        return Ok(ApiResponseFormatter.Paged(
            result.Value!.ToResponse(),
            "Notifications loaded successfully."));
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
