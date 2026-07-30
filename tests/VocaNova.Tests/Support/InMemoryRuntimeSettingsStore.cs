using System.Collections.Concurrent;
using System.Text.Json;
using VocaNova.API.Infrastructure.Configuration;

namespace VocaNova.Tests.Support;

/// <summary>
/// Stands in for the Redis-backed store. Values round-trip through JSON exactly as they would
/// in production, so serialisation mistakes still surface in tests.
/// </summary>
public sealed class InMemoryRuntimeSettingsStore : IRuntimeSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.Ordinal);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.FromResult(_values.TryGetValue(key, out var payload)
            ? JsonSerializer.Deserialize<T>(payload, JsonOptions)
            : null);
    }

    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        _values[key] = JsonSerializer.Serialize(value, JsonOptions);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _values.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
