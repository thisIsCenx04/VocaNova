using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Caching.Knn;

public sealed class RedisKnnWordRecommendationCache : IKnnWordRecommendationCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RedisSettings _settings;
    private readonly ILogger<RedisKnnWordRecommendationCache> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisKnnWordRecommendationCache(
        IOptions<RedisSettings> settings,
        ILogger<RedisKnnWordRecommendationCache> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<IReadOnlyCollection<WordRecommendationItem>?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return null;
        }

        var cached = await database.StringGetAsync(GetKey(userId));
        return cached.HasValue
            ? JsonSerializer.Deserialize<IReadOnlyCollection<WordRecommendationItem>>(cached!, JsonOptions)
            : null;
    }

    public async Task SetAsync(
        uint userId,
        IReadOnlyCollection<WordRecommendationItem> recommendations,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(recommendations, JsonOptions);
        await database.StringSetAsync(GetKey(userId), payload, ttl);
    }

    public async Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        await database.KeyDeleteAsync(GetKey(userId));
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
            _logger.LogWarning(exception, "Redis is unavailable. KNN word recommendation cache is disabled.");
            return null;
        }
    }

    private RedisKey GetKey(uint userId)
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;

        return $"{prefix}knn-words:{userId}";
    }
}
