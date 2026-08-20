using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Caching.Quiz;

public sealed class RedisQuizPoolCache : IQuizPoolCache
{
    // Dài hơn một lượt làm bài bình thường, đủ để phiên bị bỏ dở tự hết hạn.
    private static readonly TimeSpan PoolTtl = TimeSpan.FromHours(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RedisSettings _settings;
    private readonly ILogger<RedisQuizPoolCache> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisQuizPoolCache(
        IOptions<RedisSettings> settings,
        ILogger<RedisQuizPoolCache> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<IReadOnlyCollection<QuizPoolWord>?> GetAsync(
        uint sessionId,
        uint? listId,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return null;
        }

        var cached = await database.StringGetAsync(GetKey(sessionId, listId));
        var entries = cached.HasValue
            ? JsonSerializer.Deserialize<IReadOnlyCollection<QuizPoolCacheEntry>>(cached!, JsonOptions)
            : null;
        return entries?.Select(entry => entry.ToBusinessModel()).ToArray();
    }

    public async Task SetAsync(
        uint sessionId,
        uint? listId,
        IReadOnlyCollection<QuizPoolWord> pool,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(pool.Select(QuizPoolCacheEntry.From).ToArray(), JsonOptions);
        await database.StringSetAsync(GetKey(sessionId, listId), payload, PoolTtl);
    }

    public async Task RemoveAsync(
        uint sessionId,
        uint? listId,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        await database.KeyDeleteAsync(GetKey(sessionId, listId));
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
            _logger.LogWarning(exception, "Redis is unavailable. Quiz pool cache is disabled.");
            return null;
        }
    }

    /// <summary>
    /// list_id nằm trong khoá vì nó do client gửi kèm mỗi lần nộp chứ không
    /// lưu trên phiên: nếu client đổi list_id thì phải dựng lại tập từ khác,
    /// đúng như khi chưa có cache.
    /// </summary>
    private RedisKey GetKey(uint sessionId, uint? listId)
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;

        return $"{prefix}quiz-pool:{sessionId}:{listId?.ToString() ?? "all"}";
    }
}
