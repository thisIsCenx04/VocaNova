namespace VocaNova.API.Infrastructure.RateLimiting;

public interface IAdminKnnTriggerRateLimiter
{
    bool IsAllowed(uint adminUserId, DateTime now);
}
