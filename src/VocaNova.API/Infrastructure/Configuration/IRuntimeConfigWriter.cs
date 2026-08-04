namespace VocaNova.API.Infrastructure.Configuration;

public enum RuntimeConfigTarget
{
    /// <summary>Values were written to the repository's <c>.env</c> file.</summary>
    EnvFile,

    /// <summary>
    /// The <c>.env</c> file was missing or not writable (read-only deployment, container), so
    /// the values were kept in the shared fallback store instead.
    /// </summary>
    Fallback,
}

/// <summary>
/// Persists admin-editable settings. The preferred destination is <c>.env</c>, which keeps a
/// single source of truth alongside the rest of the deployment configuration and survives a
/// Redis flush; the fallback store exists so a read-only filesystem degrades instead of
/// failing the request.
/// </summary>
public interface IRuntimeConfigWriter
{
    /// <summary>
    /// Whether a writable <c>.env</c> is available right now. Used by the admin screens to show
    /// where a change will actually land.
    /// </summary>
    bool CanWriteEnvFile { get; }

    /// <summary>
    /// Writes ASP.NET configuration keys (for example <c>AiGrading__ApiKey</c>). A null value
    /// removes the key. Returns where the values ended up.
    /// </summary>
    Task<RuntimeConfigTarget> WriteAsync<T>(
        IReadOnlyDictionary<string, string?> values,
        string fallbackKey,
        T fallbackValue,
        CancellationToken cancellationToken = default)
        where T : class;
}
