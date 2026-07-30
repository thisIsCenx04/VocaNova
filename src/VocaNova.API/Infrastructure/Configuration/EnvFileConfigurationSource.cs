using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace VocaNova.API.Infrastructure.Configuration;

public sealed class EnvFileConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new EnvFileConfigurationProvider(this);
    }
}

public static class EnvFileConfigurationExtensions
{
    /// <summary>
    /// Layers <paramref name="envFilePath"/> on top of the existing configuration sources with
    /// change tracking enabled, so writes made by the admin settings screens take effect on the
    /// next read instead of at the next restart.
    /// </summary>
    public static IConfigurationBuilder AddEnvFile(
        this IConfigurationBuilder builder,
        string envFilePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(envFilePath));
        if (string.IsNullOrEmpty(directory))
        {
            return builder;
        }

        return builder.Add(new EnvFileConfigurationSource
        {
            FileProvider = new PhysicalFileProvider(directory),
            Path = Path.GetFileName(envFilePath),
            Optional = true,
            ReloadOnChange = true,
            // The file is rewritten as a temp file plus a move; a short delay lets the move
            // settle before the provider re-reads, avoiding a torn or empty read.
            ReloadDelay = 500,
        });
    }
}
