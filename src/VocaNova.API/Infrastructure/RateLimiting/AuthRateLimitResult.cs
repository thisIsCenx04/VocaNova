namespace VocaNova.API.Infrastructure.RateLimiting;

public sealed record AuthRateLimitResult(
    bool IsAllowed,
    int RetryAfterSeconds);
