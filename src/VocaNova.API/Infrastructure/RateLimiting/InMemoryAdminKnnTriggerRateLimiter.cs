using System.Collections.Concurrent;
using VocaNova.API.Features.Knn.BLL.Abstractions;

namespace VocaNova.API.Infrastructure.RateLimiting;

public sealed class InMemoryAdminKnnTriggerRateLimiter : IAdminKnnTriggerRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<uint, DateTime> _lastTriggerByAdmin = new();

    public bool IsAllowed(uint adminUserId, DateTime now)
    {
        if (!_lastTriggerByAdmin.TryGetValue(adminUserId, out var lastTrigger)
            || now - lastTrigger >= Window)
        {
            _lastTriggerByAdmin[adminUserId] = now;
            return true;
        }

        return false;
    }
}
