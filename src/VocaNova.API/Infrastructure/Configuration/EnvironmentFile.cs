namespace VocaNova.API.Infrastructure.Configuration;

public static class EnvironmentFile
{
    private const string FileName = ".env";

    /// <summary>
    /// Walks up from the running binary looking for the repository's <c>.env</c>.
    /// Returns <c>null</c> when there is none — for example inside a container, where the file
    /// is usually supplied to the orchestrator rather than mounted.
    /// </summary>
    public static string? FindPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var envPath = Path.Combine(directory.FullName, FileName);
            if (File.Exists(envPath))
            {
                return envPath;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Publishes the file's values as process environment variables. Still required because
    /// <c>DatabaseConnection</c> reads its connection string straight from the environment
    /// rather than from <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public static void LoadFromRepositoryRoot()
    {
        var envPath = FindPath();
        if (envPath is not null)
        {
            DotNetEnv.Env.Load(envPath);
        }
    }
}
