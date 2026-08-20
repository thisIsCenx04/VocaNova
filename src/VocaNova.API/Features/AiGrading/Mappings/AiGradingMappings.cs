using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.Contracts.Requests;
using VocaNova.API.Features.AiGrading.Contracts.Responses;

namespace VocaNova.API.Features.AiGrading.Mappings;

public static class AiGradingMappings
{
    public static UpdateAiGradingConfigurationCommand ToBusinessCommand(
        this UpdateAiGradingConfigRequest request) => new(request.Provider, request.Endpoint,
            request.Model, request.FallbackModels, request.ApiKey, request.MaxAttempts,
            request.RetryBaseDelayMs, request.AttemptTimeoutSeconds, request.PassThreshold);

    public static AiGradingConfigResponse ToResponse(this AiGradingConfigurationView value) =>
        new(value.Provider, value.Endpoint, value.Model, value.FallbackModels,
            value.MaxAttempts, value.RetryBaseDelayMs, value.AttemptTimeoutSeconds,
            value.PassThreshold, value.HasApiKey, value.ApiKeyHint, value.Storage,
            value.CanWriteEnvFile, value.SupportedProviders);

    public static AiGradingConnectionTestResponse ToResponse(this AiGradingConnectionTest value) =>
        new(value.Succeeded, value.Model, value.ElapsedMs, value.Message);
}
