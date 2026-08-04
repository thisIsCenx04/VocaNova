using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Services;

namespace VocaNova.API.Features.Dictionary.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/topics")]
public sealed class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;

    public TopicsController(ITopicService topicService)
    {
        _topicService = topicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTopics(CancellationToken cancellationToken)
    {
        var result = await _topicService.GetTopicsAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Topics loaded successfully.");
    }

    [HttpGet("{id:uint}/words")]
    public async Task<IActionResult> GetWords(
        [FromRoute] uint id,
        [FromQuery] TopicWordsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.GetWordsAsync(id, query, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Topic words loaded successfully.");
    }
}
