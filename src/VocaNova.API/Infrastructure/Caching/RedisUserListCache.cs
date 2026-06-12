using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

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

    public async Task<IReadOnlyCollection<UserListDto>?> GetAsync(
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
            ? JsonSerializer.Deserialize<UserListDto[]>(cached!, JsonOptions)
            : null;
    }

    public async Task SetAsync(
        uint userId,
        IReadOnlyCollection<UserListDto> lists,
        CancellationToken cancellationToken = default)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(lists.ToArray(), JsonOptions);
        await database.StringSetAsync(GetKey(userId), payload, ListsTtl);
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
            _logger.LogWarning(exception, "Redis is unavailable. User list cache is disabled.");
            return null;
        }
    }

    private RedisKey GetKey(uint userId)
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;

        return $"{prefix}user-lists:{userId}";
    }
}
