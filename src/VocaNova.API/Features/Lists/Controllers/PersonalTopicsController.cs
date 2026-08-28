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
[Route("api/personal-topics")]
public sealed class PersonalTopicsController : ControllerBase
{
    private readonly IPersonalTopicQueryService _queryService;
    private readonly IPersonalTopicMutationService _mutationService;

    public PersonalTopicsController(
        IPersonalTopicQueryService queryService,
        IPersonalTopicMutationService mutationService)
    {
        _queryService = queryService;
        _mutationService = mutationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTopics(
        [FromQuery] PersonalTopicListRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _queryService.GetTopicsAsync(
            userId,
            request.ToBusinessQuery(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Personal topics loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("{topicId:uint}/words")]
    public async Task<IActionResult> GetWords(
        [FromRoute] uint topicId,
        [FromQuery] ListWordsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _queryService.GetWordsAsync(
            userId,
            topicId,
            request.ToBusinessQuery(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Personal topic words loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("{topicId:uint}/words")]
    public async Task<IActionResult> AddWord(
        [FromRoute] uint topicId,
        [FromBody] AddPersonalTopicWordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.AddWordAsync(
            userId,
            topicId,
            request.ToBusinessCommand(),
            cancellationToken);
        return result.IsSuccess
            ? StatusCode(
                StatusCodes.Status201Created,
                ApiResponseFormatter.Created(
                    result.Value!.ToResponse(),
                    "Word added to personal topic successfully."))
            : ErrorResponse(result);
    }

    [HttpDelete("{topicId:uint}/words/{wordId:uint}")]
    public async Task<IActionResult> RemoveWord(
        [FromRoute] uint topicId,
        [FromRoute] uint wordId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _mutationService.RemoveWordAsync(
            userId,
            topicId,
            wordId,
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value,
                "Word removed from personal topic successfully."))
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
