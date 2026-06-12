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

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(
        [FromRoute] uint id,
        [FromBody] UpdateListRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<UserListDto>.Unauthorized("Unauthorized."));
        }

        var result = await _userListService.UpdateAsync(userId, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "List updated successfully.");
    }

    [HttpDelete("{id:uint}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _userListService.SoftDeleteAsync(userId, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "List deleted successfully.");
    }

    [HttpGet("{id:uint}/words")]
    public async Task<IActionResult> GetWords(
        [FromRoute] uint id,
        [FromQuery] ListWordsQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<PagedResult<ListWordDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _userListService.GetWordsAsync(userId, id, query, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "List words loaded successfully.");
    }

    [HttpPost("{id:uint}/words")]
    public async Task<IActionResult> AddWord(
        [FromRoute] uint id,
        [FromBody] AddListWordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<ListWordDto>.Unauthorized("Unauthorized."));
        }

        var result = await _userListService.AddWordAsync(userId, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.CreatedResult(result.Value!, "Word added to list successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
