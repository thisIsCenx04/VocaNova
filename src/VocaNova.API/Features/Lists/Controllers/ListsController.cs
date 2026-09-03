using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Features.Lists.BLL.Services;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Lists.Contracts.Requests;
using VocaNova.API.Features.Lists.Mappings;
using VocaNova.API.Features.Lists.BLL.Services.IServices;

namespace VocaNova.API.Features.Lists.Controllers;

[ApiController]
[Authorize]
[Route("api/lists")]
public sealed class ListsController : ControllerBase
{
    private readonly IListQueryService _queryService;
    private readonly IListMutationService _mutationService;

    public ListsController(
        IListQueryService queryService,
        IListMutationService mutationService)
    {
        _queryService = queryService;
        _mutationService = mutationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLists(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _queryService.GetListsAsync(userId, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Lists loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("{id:uint}/words")]
    public async Task<IActionResult> GetWords(
        [FromRoute] uint id,
        [FromQuery] ListWordsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _queryService.GetWordsAsync(
            userId,
            id,
            request.ToBusinessQuery(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "List words loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateListRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.CreateAsync(
            userId,
            request.ToBusinessCommand(),
            cancellationToken);
        return result.IsSuccess
            ? StatusCode(
                StatusCodes.Status201Created,
                ApiResponseFormatter.Created(
                    result.Value!.ToResponse(),
                    "List created successfully."))
            : ErrorResponse(result);
    }

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(
        [FromRoute] uint id,
        [FromBody] UpdateListRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.UpdateAsync(
            userId,
            id,
            request.ToBusinessCommand(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "List updated successfully."))
            : ErrorResponse(result);
    }

    [HttpDelete("{id:uint}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.SoftDeleteAsync(userId, id, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "List deleted successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("{id:uint}/words")]
    public async Task<IActionResult> AddWord(
        [FromRoute] uint id,
        [FromBody] AddListWordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.AddWordAsync(
            userId,
            id,
            request.ToBusinessCommand(),
            cancellationToken);
        return result.IsSuccess
            ? StatusCode(
                StatusCodes.Status201Created,
                ApiResponseFormatter.Created(
                    result.Value!.ToResponse(),
                    "Word added to list successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("{id:uint}/words/random")]
    public async Task<IActionResult> AddRandomWords(
        [FromRoute] uint id,
        [FromBody] AddRandomListWordsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.AddRandomWordsAsync(
            userId,
            id,
            request.ToBusinessCommand(),
            cancellationToken);
        return result.IsSuccess
            ? StatusCode(
                StatusCodes.Status201Created,
                ApiResponseFormatter.Created(
                    result.Value!.ToResponse(),
                    "Random words added to list successfully."))
            : ErrorResponse(result);
    }

    [HttpDelete("{id:uint}/words/{wordId:uint}")]
    public async Task<IActionResult> RemoveWord(
        [FromRoute] uint id,
        [FromRoute] uint wordId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.RemoveWordAsync(
            userId,
            id,
            wordId,
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Word removed from list successfully."))
            : ErrorResponse(result);
    }

    [HttpPatch("{id:uint}/words/{wordId:uint}/note")]
    public async Task<IActionResult> UpdateWordNote(
        [FromRoute] uint id,
        [FromRoute] uint wordId,
        [FromBody] UpdateListWordNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.UpdateWordNoteAsync(
            userId,
            id,
            wordId,
            request.ToBusinessCommand(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Word note updated successfully."))
            : ErrorResponse(result);
    }

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(
            StatusCodes.Status401Unauthorized,
            ApiResponseFormatter.Error("Unauthorized.", new[] { "Unauthorized." }));

    private ObjectResult ErrorResponse<T>(ListResult<T> result)
    {
        var statusCode = result.ErrorKind switch
        {
            ListErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ListErrorKind.NotFound => StatusCodes.Status404NotFound,
            ListErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ListErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(
            statusCode,
            ApiResponseFormatter.Error(result.Error!, new[] { result.Error! }));
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
