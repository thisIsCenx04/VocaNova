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
[Route("api/words")]
public sealed class WordsController : ControllerBase
{
    private readonly IWordReadService _wordService;

    public WordsController(IWordReadService wordService)
    {
        _wordService = wordService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] WordSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.SearchAsync(
            request.ToBusinessQuery(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Words loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("{id:uint}")]
    public async Task<IActionResult> GetById(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Word loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily(CancellationToken cancellationToken)
    {
        var result = await _wordService.GetDailyAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Daily word loaded successfully."))
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
