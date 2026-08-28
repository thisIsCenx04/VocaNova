namespace VocaNova.API.Features.Knn.BLL.Abstractions;

public interface IAdminKnnTriggerRateLimiter
{
    bool IsAllowed(uint adminUserId, DateTime now);
}
