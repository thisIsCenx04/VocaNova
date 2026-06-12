namespace VocaNova.API.Infrastructure.RateLimiting;

public interface IAuthRateLimiter
{
    AuthRateLimitResult TryAcquire(string key, int permitLimit, TimeSpan window);
}
