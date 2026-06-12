using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Services;

namespace VocaNova.API.Features.Dictionary.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/words")]
public sealed class WordsController : ControllerBase
{
    private readonly IWordService _wordService;

    public WordsController(IWordService wordService)
    {
        _wordService = wordService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] WordSearchQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.SearchAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Words loaded successfully.");
    }

    [HttpGet("{id:uint}")]
    public async Task<IActionResult> GetById(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Word loaded successfully.");
    }
}
