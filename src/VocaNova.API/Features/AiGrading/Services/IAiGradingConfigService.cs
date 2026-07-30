using VocaNova.API.Features.AiGrading.DTOs;

namespace VocaNova.API.Features.AiGrading.Services;

/// <summary>
/// Effective AI grading configuration: the appsettings values, overlaid with whatever an admin
/// has set from the dashboard.
/// </summary>
public interface IAiGradingConfigService
{
    /// <summary>
    /// Settings the grading pipeline should use for the current request.
    /// </summary>
    Task<AiGradingSettings> GetEffectiveSettingsAsync(CancellationToken cancellationToken = default);

    Task<AiGradingConfigDto> GetConfigAsync(CancellationToken cancellationToken = default);

    Task<AiGradingConfigDto> UpdateConfigAsync(
        UpdateAiGradingConfigRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the admin override so the deployment configuration applies again.
    /// </summary>
    Task<AiGradingConfigDto> ResetConfigAsync(CancellationToken cancellationToken = default);
}
