using StackExchange.Redis;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Caching.Knn;

public sealed class RedisKnnRebuildStateCache : IKnnRebuildStateCache
{
    private const string LastRebuildKey = "knn-last-rebuild";

    private readonly RedisSettings _settings;
    private readonly ILogger<RedisKnnRebuildStateCache> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisKnnRebuildStateCache(
        IOptions<RedisSettings> settings,
        ILogger<RedisKnnRebuildStateCache> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<DateTime?> GetLastRebuildAtAsync(CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return null;
        }

        var value = await database.StringGetAsync(GetKey());
        return value.HasValue && DateTime.TryParse(value!, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }

    public async Task SetLastRebuildAtAsync(DateTime rebuiltAt, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        await database.StringSetAsync(GetKey(), rebuiltAt.ToString("O"));
    }

    private async Task<IDatabase?> GetDatabaseAsync()
    {
        var connection = await _connection.Value;
        return connection?.GetDatabase();
    }

    private async Task<IConnectionMultiplexer?> ConnectAsync()
    {
        try
        {
            return await ConnectionMultiplexer.ConnectAsync(_settings.Configuration);
        }
        catch (RedisConnectionException exception)
        {
            _logger.LogWarning(exception, "Redis is unavailable. KNN rebuild state cache is disabled.");
            return null;
        }
    }

    private RedisKey GetKey()
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;

        return $"{prefix}{LastRebuildKey}";
    }
}
