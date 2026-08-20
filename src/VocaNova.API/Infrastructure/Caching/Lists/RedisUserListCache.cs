using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Caching.Lists;

public sealed class RedisUserListCache : IUserListCache
{
    private static readonly TimeSpan ListsTtl = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisUserListCache> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisUserListCache(
        IOptions<RedisSettings> settings,
        ILogger<RedisUserListCache> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<IReadOnlyCollection<UserListSummary>?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return null;
        }

        var cached = await database.StringGetAsync(GetKey(userId));
        var entries = cached.HasValue
            ? JsonSerializer.Deserialize<UserListCacheEntry[]>(cached!, JsonOptions)
            : null;
        return entries?.Select(entry => entry.ToBusinessModel()).ToArray();
    }

    public async Task SetAsync(
        uint userId,
        IReadOnlyCollection<UserListSummary> lists,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(
            lists.Select(UserListCacheEntry.FromBusinessModel).ToArray(),
            JsonOptions);
        await database.StringSetAsync(GetKey(userId), payload, ListsTtl);
    }

    public async Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is not null)
        {
            await database.KeyDeleteAsync(GetKey(userId));
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
            _logger.LogWarning(exception, "Redis is unavailable. User list cache is disabled.");
            return null;
        }
    }

    private RedisKey GetKey(uint userId)
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;
        return $"{prefix}user-lists:v2:{userId}";
    }
}
