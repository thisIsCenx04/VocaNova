namespace VocaNova.API.Infrastructure.Configuration;

public static class EnvironmentFile
{
    private const string FileName = ".env";

    public static void LoadFromRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var envPath = Path.Combine(directory.FullName, FileName);

            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
                return;
            }

            directory = directory.Parent;
        }
    }
}
