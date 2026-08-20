using VocaNova.API.Common.Models;
using VocaNova.API.Features.Notifications.BLL.Models;

namespace VocaNova.API.Features.Notifications.BLL.Abstractions;

public interface INotificationRepository
{
    Task<PagedCollection<DeletedWordReference>> ListDeletedWordsAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}
