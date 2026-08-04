namespace VocaNova.API.Infrastructure.Configuration;

/// <summary>
/// Persistence for settings an admin can change at runtime, layered on top of the values in
/// appsettings. The database schema is frozen, so overrides live in Redis instead of a
/// settings table; when no override is stored the deployment configuration is what applies.
/// </summary>
public interface IRuntimeSettingsStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
