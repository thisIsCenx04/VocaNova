using System.Globalization;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Common.Abstractions.Configuration;

namespace VocaNova.API.Features.Knn.BLL.Services;

public sealed class KnnRuntimeConfigurationService : IKnnRuntimeConfigurationService
{
    internal const string VectorWeightsKey = "knn:vector-weights";
    internal const string EnvPrefix = "Knn__Vector__";

    private readonly IRuntimeSettingsStore _fallbackStore;
    private readonly IRuntimeConfigWriter _configWriter;
    private readonly IOptionsMonitor<KnnOptions> _options;

    public KnnRuntimeConfigurationService(
        IRuntimeSettingsStore fallbackStore,
        IRuntimeConfigWriter configWriter,
        IOptionsMonitor<KnnOptions> options)
    {
        _fallbackStore = fallbackStore;
        _configWriter = configWriter;
        _options = options;
    }

    public async Task<KnnVectorOptions> GetVectorOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        // CurrentValue re-binds after the .env watcher fires, so a saved change reaches the
        // recommendation pipeline without a restart.
        var fallback = await _fallbackStore.GetAsync<KnnVectorWeights>(
            VectorWeightsKey,
            cancellationToken);

        return fallback is null ? _options.CurrentValue.Vector : ToOptions(fallback);
    }

    public async Task<KnnVectorWeights> UpdateVectorWeightsAsync(
        KnnVectorWeights weights,
        CancellationToken cancellationToken = default)
    {
        await _configWriter.WriteAsync(
            ToEnvValues(weights),
            VectorWeightsKey,
            weights,
            cancellationToken);

        return weights;
    }

    /// <summary>
    /// Writes the built-in defaults back out, so "reset" leaves an explicit, readable set of
    /// values in <c>.env</c> rather than an absence someone has to reason about.
    /// </summary>
    public async Task<KnnVectorWeights> ResetVectorWeightsAsync(
        CancellationToken cancellationToken = default)
    {
        var defaults = ToDto(new KnnVectorOptions());
        await _configWriter.WriteAsync(
            ToEnvValues(defaults),
            VectorWeightsKey,
            defaults,
            cancellationToken);

        return defaults;
    }

    public async Task<bool> HasVectorOverrideAsync(CancellationToken cancellationToken = default)
    {
        var effective = await GetVectorOptionsAsync(cancellationToken);
        return ToDto(effective) != ToDto(new KnnVectorOptions());
    }

    public async Task<RuntimeConfigTarget> GetStorageTargetAsync(
        CancellationToken cancellationToken = default)
    {
        var fallback = await _fallbackStore.GetAsync<KnnVectorWeights>(
            VectorWeightsKey,
            cancellationToken);

        return fallback is null ? RuntimeConfigTarget.EnvFile : RuntimeConfigTarget.Fallback;
    }

    public bool CanWriteEnvFile => _configWriter.CanWriteEnvFile;

    public static KnnVectorWeights ToDto(KnnVectorOptions options)
    {
        return new KnnVectorWeights(
            options.AgeRangeWeight,
            options.RegionWeight,
            options.OccupationWeight,
            options.EducationLevelWeight,
            options.LearningPurposeWeight,
            options.InterestTopicsWeight);
    }

    /// <summary>
    /// Flattens the weights into the <c>Knn__Vector__*</c> keys used by <c>.env</c>. Written
    /// with the invariant culture so a machine using ',' as the decimal separator does not emit
    /// a value the configuration binder cannot read back.
    /// </summary>
    public static Dictionary<string, string?> ToEnvValues(KnnVectorWeights weights)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [$"{EnvPrefix}AgeRangeWeight"] = Format(weights.AgeRangeWeight),
            [$"{EnvPrefix}RegionWeight"] = Format(weights.RegionWeight),
            [$"{EnvPrefix}OccupationWeight"] = Format(weights.OccupationWeight),
            [$"{EnvPrefix}EducationLevelWeight"] = Format(weights.EducationLevelWeight),
            [$"{EnvPrefix}LearningPurposeWeight"] = Format(weights.LearningPurposeWeight),
            [$"{EnvPrefix}InterestTopicsWeight"] = Format(weights.InterestTopicsWeight),
        };
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static KnnVectorOptions ToOptions(KnnVectorWeights weights)
    {
        return new KnnVectorOptions
        {
            AgeRangeWeight = weights.AgeRangeWeight,
            RegionWeight = weights.RegionWeight,
            OccupationWeight = weights.OccupationWeight,
            EducationLevelWeight = weights.EducationLevelWeight,
            LearningPurposeWeight = weights.LearningPurposeWeight,
            InterestTopicsWeight = weights.InterestTopicsWeight,
        };
    }
}
