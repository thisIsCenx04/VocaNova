using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Services;

namespace VocaNova.API.Features.Lists.Controllers;

[ApiController]
[Authorize]
[Route("api/personal-topics")]
public sealed class PersonalTopicsController : ControllerBase
{
    private readonly IPersonalTopicService _personalTopicService;

    public PersonalTopicsController(IPersonalTopicService personalTopicService)
    {
        _personalTopicService = personalTopicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTopics(
        [FromQuery] uint? wordId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(
                Result<IReadOnlyCollection<PersonalTopicDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _personalTopicService.GetTopicsAsync(userId, wordId, cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "Personal topics loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpGet("{topicId:uint}/words")]
    public async Task<IActionResult> GetWords(
        [FromRoute] uint topicId,
        [FromQuery] ListWordsQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(
                Result<PagedResult<ListWordDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _personalTopicService.GetWordsAsync(
            userId,
            topicId,
            query,
            cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "Personal topic words loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost("{topicId:uint}/words")]
    public async Task<IActionResult> AddWord(
        [FromRoute] uint topicId,
        [FromBody] AddPersonalTopicWordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<PersonalTopicDto>.Unauthorized("Unauthorized."));
        }

        var result = await _personalTopicService.AddWordAsync(
            userId,
            topicId,
            request,
            cancellationToken);
        return result.IsSuccess
            ? this.CreatedResult(result.Value!, "Word added to personal topic successfully.")
            : this.ErrorResult(result);
    }

    [HttpDelete("{topicId:uint}/words/{wordId:uint}")]
    public async Task<IActionResult> RemoveWord(
        [FromRoute] uint topicId,
        [FromRoute] uint wordId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _personalTopicService.RemoveWordAsync(
            userId,
            topicId,
            wordId,
            cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value, "Word removed from personal topic successfully.")
            : this.ErrorResult(result);
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
