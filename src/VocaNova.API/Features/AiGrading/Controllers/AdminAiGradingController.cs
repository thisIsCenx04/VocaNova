using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.AiGrading.BLL.Abstractions;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.BLL.Services;
using VocaNova.API.Features.AiGrading.Contracts.Requests;
using VocaNova.API.Features.AiGrading.Mappings;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.API.Features.AiGrading.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/settings/ai-grading")]
public sealed class AdminAiGradingController : ControllerBase
{
    private readonly IAiGradingConfigurationService _configurationService;
    private readonly IAiGradingProvider _provider;

    public AdminAiGradingController(IAiGradingConfigurationService configurationService,
        IAiGradingProvider provider)
    {
        _configurationService = configurationService;
        _provider = provider;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var result = await _configurationService.GetConfigAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(),
                "AI grading configuration loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateAiGradingConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _configurationService.UpdateConfigAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity();
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(),
            "AI grading configuration updated successfully."));
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetConfig(CancellationToken cancellationToken)
    {
        var result = await _configurationService.ResetConfigAsync(cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity();
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(),
            "AI grading configuration reset to deployment configuration."));
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var settings = await _configurationService.GetEffectiveSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return BadRequest(ApiResponseFormatter.Error("No API key is configured.", ["No API key is configured."]));

        var test = await _provider.TestConnectionAsync(settings, cancellationToken);
        return Ok(ApiResponseFormatter.Success(test.ToResponse(), test.Succeeded
            ? "AI grading connection test succeeded."
            : "AI grading connection test failed."));
    }

    private ObjectResult ErrorResponse<T>(AiGradingOperationResult<T> result) =>
        BadRequest(ApiResponseFormatter.Error(result.Error!, [result.Error!]));

    private void SetAuditEntity() =>
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "ai_grading_settings";
}
