using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Caching.Dictionary;

public sealed class RedisWordSearchCache :
    VocaNova.API.Features.Dictionary.BLL.Abstractions.IWordSearchCache
{
    private static readonly TimeSpan SearchTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisWordSearchCache> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisWordSearchCache(
        IOptions<RedisSettings> settings,
        ILogger<RedisWordSearchCache> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<PagedCollection<WordSummary>?> GetAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return null;
        var cached = await database.StringGetAsync(GetKey(cacheKey));
        if (!cached.HasValue) return null;

        var entry = JsonSerializer.Deserialize<PagedCacheEntry<WordSummaryCacheEntry>>(
            cached!,
            JsonOptions);
        return entry is null
            ? null
            : new PagedCollection<WordSummary>(
                entry.Items.Select(item => item.ToBusinessModel()).ToArray(),
                entry.Page,
                entry.Limit,
                entry.TotalItems);
    }

    public async Task SetAsync(
        string cacheKey,
        PagedCollection<WordSummary> result,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return;
        var payload = JsonSerializer.Serialize(
            new PagedCacheEntry<WordSummaryCacheEntry>(
                result.Items.Select(WordSummaryCacheEntry.FromBusinessModel).ToArray(),
                result.Page,
                result.Limit,
                result.TotalItems),
            JsonOptions);
        await database.StringSetAsync(GetKey(cacheKey), payload, SearchTtl);
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
            _logger.LogWarning(exception, "Redis is unavailable. Word search cache is disabled.");
            return null;
        }
    }

    private RedisKey GetKey(string cacheKey)
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;
        return $"{prefix}{cacheKey}";
    }
}
