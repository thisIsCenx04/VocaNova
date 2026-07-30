using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Infrastructure.Configuration;

namespace VocaNova.API.Features.Knn.Services;

/// <summary>
/// Effective KNN configuration: the appsettings values, overlaid with whatever an admin has
/// tuned from the dashboard.
/// </summary>
public interface IKnnRuntimeConfigService
{
    /// <summary>
    /// Weights currently in force for <see cref="KnnProfileVectorBuilder"/>.
    /// </summary>
    Task<KnnVectorOptions> GetVectorOptionsAsync(CancellationToken cancellationToken = default);

    Task<KnnVectorWeightsDto> UpdateVectorWeightsAsync(
        KnnVectorWeightsDto weights,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the admin override so the deployment configuration applies again.
    /// </summary>
    Task<KnnVectorWeightsDto> ResetVectorWeightsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the values in force differ from the built-in defaults.
    /// </summary>
    Task<bool> HasVectorOverrideAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Where the weights currently in force are stored.
    /// </summary>
    Task<RuntimeConfigTarget> GetStorageTargetAsync(CancellationToken cancellationToken = default);

    bool CanWriteEnvFile { get; }
}
