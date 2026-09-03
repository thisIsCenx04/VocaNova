using VocaNova.API.Features.AiGrading.BLL.Models;

namespace VocaNova.API.Features.AiGrading.BLL.Services.IServices;

public interface IAiGradingConfigurationService
{
    Task<AiGradingConfiguration> GetEffectiveSettingsAsync(CancellationToken cancellationToken = default);
    Task<AiGradingOperationResult<AiGradingConfigurationView>> GetConfigAsync(CancellationToken cancellationToken = default);
    Task<AiGradingOperationResult<AiGradingConfigurationView>> UpdateConfigAsync(
        UpdateAiGradingConfigurationCommand command, CancellationToken cancellationToken = default);
    Task<AiGradingOperationResult<AiGradingConfigurationView>> ResetConfigAsync(CancellationToken cancellationToken = default);
}
