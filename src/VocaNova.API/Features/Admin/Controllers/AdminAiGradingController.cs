using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.AiGrading;
using VocaNova.API.Features.AiGrading.DTOs;
using VocaNova.API.Features.AiGrading.Services;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.API.Features.Admin.Controllers;

/// <summary>
/// Lets an admin point quiz grading at a different provider/model/key without a redeploy.
/// The stored API key is never returned; responses only carry a masked hint.
/// </summary>
[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/settings/ai-grading")]
public sealed class AdminAiGradingController : ControllerBase
{
    private const string TestPrompt =
        """
        Return only valid JSON: {"score": 1.0, "explanation": "ok", "suggestion": null}
        """;

    private readonly IAiGradingConfigService _configService;
    private readonly IGeminiClient _geminiClient;

    public AdminAiGradingController(
        IAiGradingConfigService configService,
        IGeminiClient geminiClient)
    {
        _configService = configService;
        _geminiClient = geminiClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var config = await _configService.GetConfigAsync(cancellationToken);
        return this.OkResult(config, "AI grading configuration loaded successfully.");
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConfig(
        [FromBody] UpdateAiGradingConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Provider)
            && !AiGradingConfigService.IsSupportedProvider(request.Provider))
        {
            return this.ErrorResult(Result<AiGradingConfigDto>.Fail(
                $"Provider must be one of: {string.Join(", ", AiGradingConfigService.SupportedProviders)}."));
        }

        var config = await _configService.UpdateConfigAsync(request, cancellationToken);
        SetAuditEntity();

        return this.OkResult(config, "AI grading configuration updated successfully.");
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetConfig(CancellationToken cancellationToken)
    {
        var config = await _configService.ResetConfigAsync(cancellationToken);
        SetAuditEntity();

        return this.OkResult(config, "AI grading configuration reset to deployment configuration.");
    }

    /// <summary>
    /// Sends one throwaway prompt using the settings currently in force, so an admin can see
    /// whether the key and model actually work before learners hit a broken grader.
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var settings = await _configService.GetEffectiveSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return this.ErrorResult(Result<AiGradingConnectionTestDto>.Fail(
                "No API key is configured."));
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _geminiClient.GenerateContentAsync(TestPrompt, settings, cancellationToken);
            stopwatch.Stop();

            return this.OkResult(
                new AiGradingConnectionTestDto(
                    true,
                    settings.Model,
                    stopwatch.ElapsedMilliseconds,
                    "The provider responded successfully."),
                "AI grading connection test succeeded.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            // A failed probe is a valid answer to "does this configuration work?", so it is
            // reported as data rather than as a server error.
            return this.OkResult(
                new AiGradingConnectionTestDto(
                    false,
                    settings.Model,
                    stopwatch.ElapsedMilliseconds,
                    exception.Message),
                "AI grading connection test failed.");
        }
    }

    private void SetAuditEntity()
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "ai_grading_settings";
    }
}
