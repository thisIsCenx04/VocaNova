using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Caching.Dictionary;

public sealed class RedisTopicCache :
    VocaNova.API.Features.Dictionary.BLL.Abstractions.ITopicCache
{
    private static readonly TimeSpan TopicsTtl = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan TopicWordsTtl = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisTopicCache> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisTopicCache(
        IOptions<RedisSettings> settings,
        ILogger<RedisTopicCache> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<IReadOnlyCollection<TopicSummary>?> GetTopicsAsync(
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return null;
        var cached = await database.StringGetAsync(GetKey("topics"));
        var entries = cached.HasValue
            ? JsonSerializer.Deserialize<TopicSummaryCacheEntry[]>(cached!, JsonOptions)
            : null;
        return entries?.Select(entry => entry.ToBusinessModel()).ToArray();
    }

    public async Task SetTopicsAsync(
        IReadOnlyCollection<TopicSummary> topics,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return;
        var payload = JsonSerializer.Serialize(
            topics.Select(TopicSummaryCacheEntry.FromBusinessModel).ToArray(),
            JsonOptions);
        await database.StringSetAsync(GetKey("topics"), payload, TopicsTtl);
    }

    public async Task<PagedCollection<WordSummary>?> GetTopicWordsAsync(
        uint topicId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return null;
        var cached = await database.StringGetAsync(GetKey(GetTopicWordsKey(topicId, page, limit)));
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

    public async Task SetTopicWordsAsync(
        uint topicId,
        int page,
        int limit,
        PagedCollection<WordSummary> words,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null) return;
        var payload = JsonSerializer.Serialize(
            new PagedCacheEntry<WordSummaryCacheEntry>(
                words.Items.Select(WordSummaryCacheEntry.FromBusinessModel).ToArray(),
                words.Page,
                words.Limit,
                words.TotalItems),
            JsonOptions);
        await database.StringSetAsync(
            GetKey(GetTopicWordsKey(topicId, page, limit)),
            payload,
            TopicWordsTtl);
    }

    public async Task RemoveTopicsAsync(CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is not null) await database.KeyDeleteAsync(GetKey("topics"));
    }

    public async Task RemoveTopicWordsAsync(
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connection.Value;
        if (connection is null) return;
        var database = connection.GetDatabase();
        var pattern = GetKey($"topic-words:{topicId}:*").ToString();
        foreach (var endpoint in connection.GetEndPoints())
        {
            var server = connection.GetServer(endpoint);
            if (!server.IsConnected) continue;
            var keys = server.Keys(database.Database, pattern: pattern).ToArray();
            if (keys.Length > 0) await database.KeyDeleteAsync(keys);
        }
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
            _logger.LogWarning(exception, "Redis is unavailable. Topic cache is disabled.");
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

    private static string GetTopicWordsKey(uint topicId, int page, int limit) =>
        $"topic-words:{topicId}:{page}:{limit}";
}
