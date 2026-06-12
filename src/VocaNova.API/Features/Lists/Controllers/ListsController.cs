using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Services;

namespace VocaNova.API.Features.Lists.Controllers;

[ApiController]
[Authorize]
[Route("api/lists")]
public sealed class ListsController : ControllerBase
{
    private readonly IUserListService _userListService;

    public ListsController(IUserListService userListService)
    {
        _userListService = userListService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLists(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<IReadOnlyCollection<UserListDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _userListService.GetByUserAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Lists loaded successfully.");
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateListRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<UserListDto>.Unauthorized("Unauthorized."));
        }

        var result = await _userListService.CreateAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.CreatedResult(result.Value!, "List created successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
