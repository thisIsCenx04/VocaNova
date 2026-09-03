using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IAuthRateLimiter
{
    AuthRateLimitDecision TryAcquire(string key, int permitLimit, TimeSpan window);
}
