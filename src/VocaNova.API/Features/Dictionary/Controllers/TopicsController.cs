using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Features.Dictionary.BLL.Services.IServices;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Dictionary.Contracts.Requests;
using VocaNova.API.Features.Dictionary.Mappings;

namespace VocaNova.API.Features.Dictionary.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/topics")]
public sealed class TopicsController : ControllerBase
{
    private readonly ITopicReadService _topicService;

    public TopicsController(ITopicReadService topicService)
    {
        _topicService = topicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTopics(CancellationToken cancellationToken)
    {
        var result = await _topicService.GetTopicsAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Topics loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("{id:uint}/words")]
    public async Task<IActionResult> GetWords(
        [FromRoute] uint id,
        [FromQuery] TopicWordsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.GetWordsAsync(
            id,
            request.ToBusinessQuery(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Topic words loaded successfully."))
            : ErrorResponse(result);
    }

    private ObjectResult ErrorResponse<T>(DictionaryResult<T> result)
    {
        var statusCode = result.ErrorKind == DictionaryErrorKind.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return StatusCode(
            statusCode,
            ApiResponseFormatter.Error(result.Error!, new[] { result.Error! }));
    }
}
