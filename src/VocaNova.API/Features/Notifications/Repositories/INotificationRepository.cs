using VocaNova.API.Common.Results;

namespace VocaNova.API.Features.Notifications.Repositories;

// A "word deleted" notification for a user, derived on read from existing tables
// (words + user_list_words + user_word_progresses). Nothing is stored: the word id
// is the stable notification id and the word's soft-delete time is the created-at.
public sealed record DeletedWordRef(uint WordId, string? WordText, DateTime DeletedAt);

public interface INotificationRepository
{
    // Deleted words the user still references, newest first. Derived — no notifications table.
    Task<PagedResult<DeletedWordRef>> ListWordDeletedAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default);
}
