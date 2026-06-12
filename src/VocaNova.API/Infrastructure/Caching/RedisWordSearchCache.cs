using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public sealed class RedisWordSearchCache : IWordSearchCache
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

    public async Task<PagedResult<WordSummaryDto>?> GetAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return null;
        }

        var cached = await database.StringGetAsync(GetKey(cacheKey));
        if (!cached.HasValue)
        {
            return null;
        }

        var cachedResult = JsonSerializer.Deserialize<CachedPagedResult>(cached!, JsonOptions);
        return cachedResult is null
            ? null
            : new PagedResult<WordSummaryDto>(
                cachedResult.Items,
                cachedResult.Page,
                cachedResult.Limit,
                cachedResult.TotalItems);
    }

    public async Task SetAsync(
        string cacheKey,
        PagedResult<WordSummaryDto> result,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(
            new CachedPagedResult(
                result.Items.ToArray(),
                result.Page,
                result.Limit,
                result.TotalItems),
            JsonOptions);
        await database.StringSetAsync(GetKey(cacheKey), payload, SearchTtl);
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

    private sealed record CachedPagedResult(
        WordSummaryDto[] Items,
        int Page,
        int Limit,
        int TotalItems);
}
