using System.Globalization;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.AiGrading.DTOs;
using VocaNova.API.Infrastructure.Configuration;

namespace VocaNova.API.Features.AiGrading.Services;

public sealed class AiGradingConfigService : IAiGradingConfigService
{
    internal const string ConfigKey = "ai-grading:settings";
    internal const string EnvPrefix = "AiGrading__";

    /// <summary>
    /// Stale <c>FallbackModels__N</c> entries have to be deleted explicitly, otherwise shrinking
    /// the list would leave orphans behind in the file. The validator caps the list well below
    /// this bound.
    /// </summary>
    private const int MaxFallbackModelSlots = 10;

    /// <summary>
    /// Providers with a working client implementation. Accepting anything else would leave
    /// grading silently broken, so the value is validated rather than stored blindly.
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedProviders = ["Gemini"];

    private readonly IRuntimeSettingsStore _fallbackStore;
    private readonly IRuntimeConfigWriter _configWriter;
    private readonly IOptionsMonitor<AiGradingSettings> _configuredSettings;

    public AiGradingConfigService(
        IRuntimeSettingsStore fallbackStore,
        IRuntimeConfigWriter configWriter,
        IOptionsMonitor<AiGradingSettings> configuredSettings)
    {
        _fallbackStore = fallbackStore;
        _configWriter = configWriter;
        _configuredSettings = configuredSettings;
    }

    public async Task<AiGradingSettings> GetEffectiveSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        // CurrentValue re-binds after the .env watcher fires, so a saved change is visible
        // here without a restart. The fallback store only holds anything when the file could
        // not be written.
        var fromConfiguration = Normalize(_configuredSettings.CurrentValue);
        var fallback = await _fallbackStore.GetAsync<AiGradingSettings>(ConfigKey, cancellationToken);

        return fallback is null ? fromConfiguration : Normalize(fallback);
    }

    public async Task<AiGradingConfigDto> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var fallback = await _fallbackStore.GetAsync<AiGradingSettings>(ConfigKey, cancellationToken);
        var effective = fallback is null
            ? Normalize(_configuredSettings.CurrentValue)
            : Normalize(fallback);

        return MapConfig(
            effective,
            fallback is null ? RuntimeConfigTarget.EnvFile : RuntimeConfigTarget.Fallback);
    }

    public async Task<AiGradingConfigDto> UpdateConfigAsync(
        UpdateAiGradingConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var current = await GetEffectiveSettingsAsync(cancellationToken);
        var updated = Normalize(new AiGradingSettings
        {
            Provider = Coalesce(request.Provider, current.Provider),
            Endpoint = Coalesce(request.Endpoint, current.Endpoint),
            Model = Coalesce(request.Model, current.Model),
            FallbackModels = request.FallbackModels is null
                ? current.FallbackModels
                : CleanModelList(request.FallbackModels),
            // A blank key means "leave the existing secret alone" so the form can be saved
            // without the admin re-typing it.
            ApiKey = Coalesce(request.ApiKey, current.ApiKey),
            MaxAttempts = request.MaxAttempts ?? current.MaxAttempts,
            RetryBaseDelayMs = request.RetryBaseDelayMs ?? current.RetryBaseDelayMs,
            AttemptTimeoutSeconds = request.AttemptTimeoutSeconds ?? current.AttemptTimeoutSeconds,
            PassThreshold = request.PassThreshold ?? current.PassThreshold,
        });

        var target = await _configWriter.WriteAsync(
            ToEnvValues(updated),
            ConfigKey,
            updated,
            cancellationToken);

        return MapConfig(updated, target);
    }

    /// <summary>
    /// Restores the built-in defaults for everything except the API key: wiping the credential
    /// would take grading offline, which is never what "reset the tuning" is meant to do.
    /// </summary>
    public async Task<AiGradingConfigDto> ResetConfigAsync(CancellationToken cancellationToken = default)
    {
        var current = await GetEffectiveSettingsAsync(cancellationToken);
        var defaults = new AiGradingSettings { ApiKey = current.ApiKey };
        var normalized = Normalize(defaults);

        var target = await _configWriter.WriteAsync(
            ToEnvValues(normalized),
            ConfigKey,
            normalized,
            cancellationToken);

        return MapConfig(normalized, target);
    }

    public static bool IsSupportedProvider(string? provider)
    {
        return provider is not null
            && SupportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Flattens the settings into the <c>AiGrading__*</c> keys used by <c>.env</c>. Numbers are
    /// written with the invariant culture so a machine using ',' as the decimal separator does
    /// not emit a value the configuration binder cannot read back.
    /// </summary>
    public static Dictionary<string, string?> ToEnvValues(AiGradingSettings settings)
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

        var fallbackModels = settings.FallbackModels ?? [];
        for (var index = 0; index < MaxFallbackModelSlots; index++)
        {
            values[$"{EnvPrefix}FallbackModels__{index}"] = index < fallbackModels.Length
                ? fallbackModels[index]
                : null;
        }

        return values;
    }

    private AiGradingConfigDto MapConfig(AiGradingSettings settings, RuntimeConfigTarget target)
    {
        return new AiGradingConfigDto(
            settings.Provider,
            settings.Endpoint,
            settings.Model,
            settings.FallbackModels ?? [],
            settings.MaxAttempts,
            settings.RetryBaseDelayMs,
            settings.AttemptTimeoutSeconds,
            settings.PassThreshold,
            !string.IsNullOrWhiteSpace(settings.ApiKey),
            MaskApiKey(settings.ApiKey),
            target == RuntimeConfigTarget.EnvFile ? "env_file" : "fallback",
            _configWriter.CanWriteEnvFile,
            SupportedProviders);
    }

    /// <summary>
    /// Shows just enough of the key for an admin to tell which one is configured, without
    /// handing the secret back over the wire.
    /// </summary>
    private static string? MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var trimmed = apiKey.Trim();
        return trimmed.Length <= 4
            ? new string('•', trimmed.Length)
            : $"{new string('•', 8)}{trimmed[^4..]}";
    }

    private static AiGradingSettings Normalize(AiGradingSettings settings)
    {
        return new AiGradingSettings
        {
            Provider = string.IsNullOrWhiteSpace(settings.Provider)
                ? SupportedProviders[0]
                : settings.Provider.Trim(),
            Endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
                ? new AiGradingSettings().Endpoint
                : settings.Endpoint.Trim().TrimEnd('/'),
            Model = settings.Model?.Trim() ?? string.Empty,
            FallbackModels = CleanModelList(settings.FallbackModels),
            ApiKey = settings.ApiKey?.Trim() ?? string.Empty,
            MaxAttempts = Math.Clamp(settings.MaxAttempts, 1, 4),
            RetryBaseDelayMs = Math.Clamp(settings.RetryBaseDelayMs, 0, 5_000),
            AttemptTimeoutSeconds = Math.Clamp(settings.AttemptTimeoutSeconds, 1, 15),
            PassThreshold = Math.Clamp(settings.PassThreshold, 0.0, 1.0),
        };
    }

    private static string[] CleanModelList(IEnumerable<string>? models)
    {
        return (models ?? [])
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Coalesce(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
