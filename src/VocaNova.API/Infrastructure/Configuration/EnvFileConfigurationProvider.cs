using Microsoft.Extensions.Configuration;

namespace VocaNova.API.Infrastructure.Configuration;

/// <summary>
/// Reads a <c>.env</c> file into <see cref="IConfiguration"/>.
///
/// The process-environment provider that <c>DotNetEnv</c> feeds has no file watcher, so values
/// loaded that way are frozen for the lifetime of the process. Layering the same file in as a
/// real file-based source means an admin edit to <c>.env</c> is picked up without a restart.
/// </summary>
public sealed class EnvFileConfigurationProvider : FileConfigurationProvider
{
    public EnvFileConfigurationProvider(EnvFileConfigurationSource source)
        : base(source)
    {
    }

    public override void Load(Stream stream)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            if (!TryParseLine(line, out var key, out var value))
            {
                continue;
            }

            // ASP.NET expresses nesting with ':'; .env uses '__' because ':' is not portable
            // across shells.
            data[key.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal)] = value;
        }

        Data = data;
    }

    /// <summary>
    /// Accepts <c>KEY=VALUE</c>, optionally prefixed with <c>export</c> and optionally quoted,
    /// matching what DotNetEnv already parses for this project's file.
    /// </summary>
    public static bool TryParseLine(string line, out string key, out string? value)
    {
        key = string.Empty;
        value = null;

        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        if (trimmed.StartsWith("export ", StringComparison.Ordinal))
        {
            trimmed = trimmed["export ".Length..].TrimStart();
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return false;
        }

        key = trimmed[..separatorIndex].Trim();
        if (key.Length == 0)
        {
            return false;
        }

        value = Unquote(trimmed[(separatorIndex + 1)..].Trim());
        return true;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
