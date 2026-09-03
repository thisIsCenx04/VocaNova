using System.Collections.Concurrent;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Infrastructure.RateLimiting;

public sealed class InMemoryAuthRateLimiter : IAuthRateLimiter
{
    private readonly ConcurrentDictionary<string, WindowCounter> _counters = new(StringComparer.Ordinal);
    private readonly object _syncRoot = new();

    public AuthRateLimitDecision TryAcquire(string key, int permitLimit, TimeSpan window)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);

        var now = DateTimeOffset.UtcNow;

        lock (_syncRoot)
        {
            var counter = _counters.GetOrAdd(key, _ => new WindowCounter(now.Add(window), 0));
            if (counter.ResetAt <= now)
            {
                counter.ResetAt = now.Add(window);
                counter.Count = 0;
            }

            if (counter.Count >= permitLimit)
            {
                var retryAfter = (int)Math.Ceiling((counter.ResetAt - now).TotalSeconds);
                return new AuthRateLimitDecision(false, Math.Max(retryAfter, 1));
            }

            counter.Count++;
            return new AuthRateLimitDecision(true, 0);
        }
    }

    private sealed class WindowCounter
    {
        public WindowCounter(DateTimeOffset resetAt, int count)
        {
            ResetAt = resetAt;
            Count = count;
        }

        public DateTimeOffset ResetAt { get; set; }

        public int Count { get; set; }
    }
}
