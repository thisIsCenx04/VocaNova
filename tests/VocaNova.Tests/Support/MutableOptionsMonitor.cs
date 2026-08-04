using Microsoft.Extensions.Options;

namespace VocaNova.Tests.Support;

/// <summary>
/// An <see cref="IOptionsMonitor{T}"/> whose value can be swapped, standing in for the .env
/// file watcher re-binding configuration after a write.
/// </summary>
public sealed class MutableOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly List<Action<T, string?>> _listeners = [];

    public MutableOptionsMonitor(T value)
    {
        CurrentValue = value;
    }

    public T CurrentValue { get; private set; }

    public T Get(string? name) => CurrentValue;

    /// <summary>Simulates the configuration reload that follows a .env write.</summary>
    public void Set(T value)
    {
        CurrentValue = value;
        foreach (var listener in _listeners.ToArray())
        {
            listener(value, Options.DefaultName);
        }
    }

    public IDisposable OnChange(Action<T, string?> listener)
    {
        _listeners.Add(listener);
        return new Subscription(() => _listeners.Remove(listener));
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _dispose;

        public Subscription(Action dispose) => _dispose = dispose;

        public void Dispose() => _dispose();
    }
}
