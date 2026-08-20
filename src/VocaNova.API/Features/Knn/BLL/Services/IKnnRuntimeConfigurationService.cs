using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Common.Abstractions.Configuration;

namespace VocaNova.API.Features.Knn.BLL.Services;

/// <summary>
/// Effective KNN configuration: the appsettings values, overlaid with whatever an admin has
/// tuned from the dashboard.
/// </summary>
public interface IKnnRuntimeConfigurationService
{
    /// <summary>
    /// Weights currently in force for <see cref="KnnProfileVectorBuilder"/>.
    /// </summary>
    Task<KnnVectorOptions> GetVectorOptionsAsync(CancellationToken cancellationToken = default);

    Task<KnnVectorWeights> UpdateVectorWeightsAsync(
        KnnVectorWeights weights,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the admin override so the deployment configuration applies again.
    /// </summary>
    Task<KnnVectorWeights> ResetVectorWeightsAsync(CancellationToken cancellationToken = default);

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
