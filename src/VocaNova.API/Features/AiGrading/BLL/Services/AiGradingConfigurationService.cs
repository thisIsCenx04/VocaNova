using System.Globalization;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Abstractions.Configuration;
using VocaNova.API.Features.AiGrading.BLL.Models;

namespace VocaNova.API.Features.AiGrading.BLL.Services;

public sealed class AiGradingConfigurationService : IAiGradingConfigurationService
{
    internal const string ConfigKey = "ai-grading:settings";
    internal const string EnvPrefix = "AiGrading__";
    private const int MaxFallbackModelSlots = 10;
    public static readonly IReadOnlyList<string> SupportedProviders = ["Gemini"];

    private readonly IRuntimeSettingsStore _fallbackStore;
    private readonly IRuntimeConfigWriter _configWriter;
    private readonly IOptionsMonitor<AiGradingConfiguration> _configuredSettings;

    public AiGradingConfigurationService(IRuntimeSettingsStore fallbackStore,
        IRuntimeConfigWriter configWriter,
        IOptionsMonitor<AiGradingConfiguration> configuredSettings)
    {
        _fallbackStore = fallbackStore;
        _configWriter = configWriter;
        _configuredSettings = configuredSettings;
    }

    public async Task<AiGradingConfiguration> GetEffectiveSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var configured = Normalize(_configuredSettings.CurrentValue);
        var fallback = await _fallbackStore.GetAsync<AiGradingConfiguration>(ConfigKey, cancellationToken);
        return fallback is null ? configured : Normalize(fallback);
    }

    public async Task<AiGradingOperationResult<AiGradingConfigurationView>> GetConfigAsync(
        CancellationToken cancellationToken = default)
    {
        var fallback = await _fallbackStore.GetAsync<AiGradingConfiguration>(ConfigKey, cancellationToken);
        var effective = fallback is null ? Normalize(_configuredSettings.CurrentValue) : Normalize(fallback);
        return AiGradingOperationResult<AiGradingConfigurationView>.Success(MapConfig(effective,
            fallback is null ? RuntimeConfigTarget.EnvFile : RuntimeConfigTarget.Fallback));
    }

    public async Task<AiGradingOperationResult<AiGradingConfigurationView>> UpdateConfigAsync(
        UpdateAiGradingConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.Provider) && !IsSupportedProvider(command.Provider))
        {
            return AiGradingOperationResult<AiGradingConfigurationView>.ValidationFailure(
                $"Provider must be one of: {string.Join(", ", SupportedProviders)}.");
        }

        var current = await GetEffectiveSettingsAsync(cancellationToken);
        var updated = Normalize(new AiGradingConfiguration
        {
            Provider = Coalesce(command.Provider, current.Provider),
            Endpoint = Coalesce(command.Endpoint, current.Endpoint),
            Model = Coalesce(command.Model, current.Model),
            FallbackModels = command.FallbackModels is null ? current.FallbackModels : CleanModelList(command.FallbackModels),
            ApiKey = Coalesce(command.ApiKey, current.ApiKey),
            MaxAttempts = command.MaxAttempts ?? current.MaxAttempts,
            RetryBaseDelayMs = command.RetryBaseDelayMs ?? current.RetryBaseDelayMs,
            AttemptTimeoutSeconds = command.AttemptTimeoutSeconds ?? current.AttemptTimeoutSeconds,
            PassThreshold = command.PassThreshold ?? current.PassThreshold,
        });
        var target = await _configWriter.WriteAsync(ToEnvValues(updated), ConfigKey, updated, cancellationToken);
        return AiGradingOperationResult<AiGradingConfigurationView>.Success(MapConfig(updated, target));
    }

    public async Task<AiGradingOperationResult<AiGradingConfigurationView>> ResetConfigAsync(
        CancellationToken cancellationToken = default)
    {
        var current = await GetEffectiveSettingsAsync(cancellationToken);
        var defaults = Normalize(new AiGradingConfiguration { ApiKey = current.ApiKey });
        var target = await _configWriter.WriteAsync(ToEnvValues(defaults), ConfigKey, defaults, cancellationToken);
        return AiGradingOperationResult<AiGradingConfigurationView>.Success(MapConfig(defaults, target));
    }

    public static bool IsSupportedProvider(string? provider) => provider is not null
        && SupportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase);

    public static Dictionary<string, string?> ToEnvValues(AiGradingConfiguration settings)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [$"{EnvPrefix}Provider"] = settings.Provider,
            [$"{EnvPrefix}Endpoint"] = settings.Endpoint,
            [$"{EnvPrefix}Model"] = settings.Model,
            [$"{EnvPrefix}ApiKey"] = settings.ApiKey,
            [$"{EnvPrefix}MaxAttempts"] = settings.MaxAttempts.ToString(CultureInfo.InvariantCulture),
            [$"{EnvPrefix}RetryBaseDelayMs"] = settings.RetryBaseDelayMs.ToString(CultureInfo.InvariantCulture),
            [$"{EnvPrefix}AttemptTimeoutSeconds"] = settings.AttemptTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            [$"{EnvPrefix}PassThreshold"] = settings.PassThreshold.ToString(CultureInfo.InvariantCulture),
        };
        var models = settings.FallbackModels ?? [];
        for (var index = 0; index < MaxFallbackModelSlots; index++)
            values[$"{EnvPrefix}FallbackModels__{index}"] = index < models.Length ? models[index] : null;
        return values;
    }

    private AiGradingConfigurationView MapConfig(AiGradingConfiguration settings, RuntimeConfigTarget target) =>
        new(settings.Provider, settings.Endpoint, settings.Model, settings.FallbackModels ?? [],
            settings.MaxAttempts, settings.RetryBaseDelayMs, settings.AttemptTimeoutSeconds,
            settings.PassThreshold, !string.IsNullOrWhiteSpace(settings.ApiKey), MaskApiKey(settings.ApiKey),
            target == RuntimeConfigTarget.EnvFile ? "env_file" : "fallback",
            _configWriter.CanWriteEnvFile, SupportedProviders);

    private static string? MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var trimmed = apiKey.Trim();
        return trimmed.Length <= 4 ? new string('•', trimmed.Length) : $"{new string('•', 8)}{trimmed[^4..]}";
    }

    private static AiGradingConfiguration Normalize(AiGradingConfiguration settings) => new()
    {
        Provider = string.IsNullOrWhiteSpace(settings.Provider) ? SupportedProviders[0] : settings.Provider.Trim(),
        Endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? new AiGradingConfiguration().Endpoint : settings.Endpoint.Trim().TrimEnd('/'),
        Model = settings.Model?.Trim() ?? string.Empty,
        FallbackModels = CleanModelList(settings.FallbackModels),
        ApiKey = settings.ApiKey?.Trim() ?? string.Empty,
        MaxAttempts = Math.Clamp(settings.MaxAttempts, 1, 4),
        RetryBaseDelayMs = Math.Clamp(settings.RetryBaseDelayMs, 0, 5_000),
        AttemptTimeoutSeconds = Math.Clamp(settings.AttemptTimeoutSeconds, 1, 15),
        PassThreshold = Math.Clamp(settings.PassThreshold, 0.0, 1.0),
    };

    private static string[] CleanModelList(IEnumerable<string>? models) => (models ?? [])
        .Where(model => !string.IsNullOrWhiteSpace(model)).Select(model => model.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static string Coalesce(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
