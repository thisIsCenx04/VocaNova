using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VocaNova.API.Common.Abstractions.Configuration;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Infrastructure.Configuration;

/// <summary>
/// Redis-backed runtime settings, written without an expiry so an admin change survives
/// restarts. Every value is also mirrored in a process-local dictionary: if Redis is down the
/// change still takes effect on this instance rather than being silently dropped, and reads
/// stay cheap on the hot paths that consume these settings.
/// </summary>
public sealed class RedisRuntimeSettingsStore : IRuntimeSettingsStore
{
    private const string KeyPrefix = "runtime-settings:";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, string> _localValues = new(StringComparer.Ordinal);
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisRuntimeSettingsStore> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisRuntimeSettingsStore(
        IOptions<RedisSettings> settings,
        ILogger<RedisRuntimeSettingsStore> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectAsync);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        var payload = await ReadRawAsync(key);
        if (payload is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Stored runtime setting {Key} could not be parsed; falling back to configuration.",
                key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        var payload = JsonSerializer.Serialize(value, JsonOptions);
        _localValues[key] = payload;

        var database = await GetDatabaseAsync();
        if (database is null)
        {
            _logger.LogWarning(
                "Redis is unavailable; runtime setting {Key} was applied to this instance only.",
                key);
            return;
        }

        await database.StringSetAsync(GetKey(key), payload);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _localValues.TryRemove(key, out _);

        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return;
        }

        await database.KeyDeleteAsync(GetKey(key));
    }

    private async Task<string?> ReadRawAsync(string key)
    {
        var database = await GetDatabaseAsync();
        if (database is not null)
        {
            var value = await database.StringGetAsync(GetKey(key));
            if (value.HasValue)
            {
                // Keep the local mirror aligned so a later Redis outage does not roll the
                // setting back to the appsettings default.
                _localValues[key] = value!;
                return value!;
            }

            // Redis is reachable and the key is genuinely absent: the override was reset.
            _localValues.TryRemove(key, out _);
            return null;
        }

        return _localValues.GetValueOrDefault(key);
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
            _logger.LogWarning(
                exception,
                "Redis is unavailable. Runtime settings fall back to deployment configuration.");
            return null;
        }
    }

    private RedisKey GetKey(string key)
    {
        var prefix = string.IsNullOrWhiteSpace(_settings.InstanceName)
            ? "vocanova:"
            : _settings.InstanceName;

        return $"{prefix}{KeyPrefix}{key}";
    }
}
