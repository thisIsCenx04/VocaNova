using Microsoft.EntityFrameworkCore;

namespace VocaNova.API.Infrastructure.Persistence;

public static class DatabaseConnection
{
    private const string ConnectionStringKey = "MYSQL_CONNECTION_STRING";
    private const string ServerVersionKey = "MYSQL_SERVER_VERSION";
    private const string DefaultServerVersion = "8.0.0-mysql";

    public static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringKey);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Missing {ConnectionStringKey}. Add it to the repository root .env file.");
        }

        return connectionString;
    }

    public static ServerVersion GetServerVersion()
    {
        var serverVersion = Environment.GetEnvironmentVariable(ServerVersionKey);

        if (string.Equals(serverVersion, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return ServerVersion.AutoDetect(GetConnectionString());
        }

        return ServerVersion.Parse(string.IsNullOrWhiteSpace(serverVersion)
            ? DefaultServerVersion
            : serverVersion);
    }
}

