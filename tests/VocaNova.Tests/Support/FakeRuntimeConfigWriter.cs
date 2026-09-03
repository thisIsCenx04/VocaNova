using VocaNova.API.Common.Abstractions.Configuration;
using VocaNova.API.Infrastructure.Configuration;

namespace VocaNova.Tests.Support;

/// <summary>
/// Stands in for the .env writer. <see cref="CanWriteEnvFile"/> can be flipped to exercise the
/// read-only-filesystem path, where settings have to land in the fallback store instead.
/// </summary>
public sealed class FakeRuntimeConfigWriter : IRuntimeConfigWriter
{
    private readonly IRuntimeSettingsStore _fallbackStore;

    public FakeRuntimeConfigWriter(IRuntimeSettingsStore fallbackStore, bool canWriteEnvFile = true)
    {
        _fallbackStore = fallbackStore;
        CanWriteEnvFile = canWriteEnvFile;
    }

    public bool CanWriteEnvFile { get; set; }

    /// <summary>Config keys of the last successful .env write, mirroring what the file would hold.</summary>
    public Dictionary<string, string?> WrittenValues { get; } = new(StringComparer.OrdinalIgnoreCase);

    public int WriteCount { get; private set; }

    public async Task<RuntimeConfigTarget> WriteAsync<T>(
        IReadOnlyDictionary<string, string?> values,
        string fallbackKey,
        T fallbackValue,
        CancellationToken cancellationToken = default)
        where T : class
    {
        WriteCount++;

        if (!CanWriteEnvFile)
        {
            await _fallbackStore.SetAsync(fallbackKey, fallbackValue, cancellationToken);
            return RuntimeConfigTarget.Fallback;
        }

        foreach (var (key, value) in values)
        {
            if (value is null)
            {
                WrittenValues.Remove(key);
            }
            else
            {
                WrittenValues[key] = value;
            }
        }

        await _fallbackStore.RemoveAsync(fallbackKey, cancellationToken);
        return RuntimeConfigTarget.EnvFile;
    }
}
