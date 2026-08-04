using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public sealed class RedisWordDetailCache : IWordDetailCache
{
    private static readonly TimeSpan DetailTtl = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RedisSettings _settings;
    private readonly ILogger<RedisWordDetailCache> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisWordDetailCache(
        IOptions<RedisSettings> settings,
        ILogger<RedisWordDetailCache> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<WordDetailDto?> GetAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return null;
        }

        var cached = await database.StringGetAsync(GetKey(wordId));
        return cached.HasValue
            ? JsonSerializer.Deserialize<WordDetailDto>(cached!, JsonOptions)
            : null;
    }

    public async Task SetAsync(WordDetailDto word, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(word, JsonOptions);
        await database.StringSetAsync(GetKey(word.WordId), payload, DetailTtl);
    }

    public async Task RemoveAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        await database.KeyDeleteAsync(GetKey(wordId));
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
            _logger.LogWarning(exception, "Redis is unavailable. Word detail cache is disabled.");
            return null;
        }
    }

    private RedisKey GetKey(uint wordId)
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;

        return $"{prefix}word:{wordId}";
    }
}
