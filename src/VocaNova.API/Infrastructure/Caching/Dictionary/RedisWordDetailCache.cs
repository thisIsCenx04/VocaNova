using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Caching.Dictionary;

public sealed class RedisWordDetailCache :
    VocaNova.API.Features.Dictionary.BLL.Abstractions.IWordDetailCache
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

    public async Task<WordDetail?> GetAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return null;
        var cached = await database.StringGetAsync(GetKey(wordId));
        return cached.HasValue
            ? JsonSerializer.Deserialize<WordDetailCacheEntry>(cached!, JsonOptions)?.ToBusinessModel()
            : null;
    }

    public async Task SetAsync(
        WordDetail word,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return;
        var payload = JsonSerializer.Serialize(
            WordDetailCacheEntry.FromBusinessModel(word),
            JsonOptions);
        await database.StringSetAsync(GetKey(word.WordId), payload, DetailTtl);
    }

    public async Task RemoveAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is not null) await database.KeyDeleteAsync(GetKey(wordId));
    }

    private async Task<IDatabase?> GetDatabaseAsync() =>
        (await _connection.Value)?.GetDatabase();

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
