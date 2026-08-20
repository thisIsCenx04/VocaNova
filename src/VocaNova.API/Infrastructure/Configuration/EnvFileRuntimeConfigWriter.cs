using System.Text;
using VocaNova.API.Common.Abstractions.Configuration;

namespace VocaNova.API.Infrastructure.Configuration;

public sealed class EnvFileRuntimeConfigWriter : IRuntimeConfigWriter
{
    private const string ManagedSectionHeader = "# --- Managed from the admin dashboard ---";

    // One writer at a time: two admins saving concurrently would otherwise interleave a
    // read-modify-write over the same file and lose one of the changes.
    private static readonly SemaphoreSlim WriteLock = new(1, 1);

    private readonly IRuntimeSettingsStore _fallbackStore;
    private readonly ILogger<EnvFileRuntimeConfigWriter> _logger;

    public EnvFileRuntimeConfigWriter(
        IRuntimeSettingsStore fallbackStore,
        ILogger<EnvFileRuntimeConfigWriter> logger)
    {
        _fallbackStore = fallbackStore;
        _logger = logger;
    }

    public bool CanWriteEnvFile
    {
        get
        {
            var path = EnvironmentFile.FindPath();
            return path is not null && IsWritable(path);
        }
    }

    public async Task<RuntimeConfigTarget> WriteAsync<T>(
        IReadOnlyDictionary<string, string?> values,
        string fallbackKey,
        T fallbackValue,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var path = EnvironmentFile.FindPath();
        if (path is not null && await TryWriteEnvFileAsync(path, values, cancellationToken))
        {
            // Configuration now carries the values, so a stale fallback entry would silently
            // shadow them on the next read.
            await _fallbackStore.RemoveAsync(fallbackKey, cancellationToken);
            return RuntimeConfigTarget.EnvFile;
        }

        _logger.LogWarning(
            "Could not write {EnvFile}; keeping the settings for {FallbackKey} in the fallback store.",
            path ?? ".env",
            fallbackKey);
        await _fallbackStore.SetAsync(fallbackKey, fallbackValue, cancellationToken);
        return RuntimeConfigTarget.Fallback;
    }

    private async Task<bool> TryWriteEnvFileAsync(
        string path,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken)
    {
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            var lines = (await File.ReadAllLinesAsync(path, cancellationToken)).ToList();
            var updated = ApplyValues(lines, values);

            // Written to a sibling temp file then moved, so a watcher never observes a
            // half-written .env and the original survives a crash mid-write.
            var tempPath = path + ".tmp";
            await File.WriteAllLinesAsync(tempPath, updated, new UTF8Encoding(false), cancellationToken);
            File.Move(tempPath, path, overwrite: true);
            return true;
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogWarning(exception, "Writing {EnvFile} failed.", path);
            return false;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    /// <summary>
    /// Rewrites matching keys in place so comments, ordering and unrelated entries survive.
    /// Keys that are not present yet are appended under a marked section; a null value deletes
    /// the line (used to drop trailing array indexes such as <c>FallbackModels__2</c>).
    /// </summary>
    public static List<string> ApplyValues(
        List<string> lines,
        IReadOnlyDictionary<string, string?> values)
    {
        var remaining = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(lines.Count + remaining.Count);

        foreach (var line in lines)
        {
            if (!EnvFileConfigurationProvider.TryParseLine(line, out var key, out _)
                || !remaining.TryGetValue(key, out var newValue))
            {
                result.Add(line);
                continue;
            }

            remaining.Remove(key);
            if (newValue is not null)
            {
                result.Add(FormatLine(key, newValue));
            }
        }

        var additions = remaining
            .Where(entry => entry.Value is not null)
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToArray();
        if (additions.Length == 0)
        {
            return result;
        }

        if (!result.Contains(ManagedSectionHeader, StringComparer.Ordinal))
        {
            if (result.Count > 0 && !string.IsNullOrWhiteSpace(result[^1]))
            {
                result.Add(string.Empty);
            }

            result.Add(ManagedSectionHeader);
        }

        result.AddRange(additions.Select(entry => FormatLine(entry.Key, entry.Value!)));
        return result;
    }

    private static string FormatLine(string key, string value)
    {
        // A value containing whitespace, quotes or a '#' would otherwise be re-read as a
        // truncated value or a comment.
        var needsQuoting = value.Length != value.Trim().Length
            || value.Contains('#', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal);

        return needsQuoting
            ? $"{key}=\"{value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)}\""
            : $"{key}={value}";
    }

    private static bool IsWritable(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return true;
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}
