namespace VocaNova.API.Features.SuperAdmin.Services;

public interface IAdminUserAssignmentStore
{
    Task<IReadOnlyDictionary<uint, IReadOnlyCollection<uint>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetUserIdsAsync(
        uint adminId,
        CancellationToken cancellationToken = default);

    Task ReplaceAsync(
        uint adminId,
        IReadOnlyCollection<uint> userIds,
        CancellationToken cancellationToken = default);
}
