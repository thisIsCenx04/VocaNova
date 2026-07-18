using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Notifications.DTOs;
using VocaNova.API.Features.Notifications.Repositories;

namespace VocaNova.API.Features.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    public const string WordDeletedType = "word_deleted";

    private const string WordDeletedTitle = "Từ vựng đã bị gỡ";

    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<NotificationDto>>> ListAsync(uint userId, NotificationListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<NotificationDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var paged = await _repository.ListWordDeletedAsync(userId, page, query.Limit, cancellationToken);
        var items = paged.Items.Select(Map).ToList();

        return Result<PagedResult<NotificationDto>>.Ok(
            new PagedResult<NotificationDto>(items, paged.Page, paged.Limit, paged.TotalItems));
    }

    // Read/unread state is tracked per-device on the client, so the server always emits is_read=false;
    // the notification is derived from the word, using its id and soft-delete time.
    private static NotificationDto Map(DeletedWordRef word)
    {
        var displayWord = string.IsNullOrWhiteSpace(word.WordText) ? $"#{word.WordId}" : word.WordText!;

        // End-user (learner) facing content — Vietnamese; `type`/`ref_id` let the app localize/link.
        var message = $"Từ \"{displayWord}\" đã bị gỡ khỏi từ điển. Nội dung liên quan trong danh sách của bạn có thể không còn khả dụng.";

        return new NotificationDto(
            NotificationId: word.WordId,
            Type: WordDeletedType,
            Title: WordDeletedTitle,
            Message: message,
            RefType: "word",
            RefId: word.WordId,
            IsRead: false,
            CreatedAt: word.DeletedAt,
            ReadAt: null);
    }
}
