using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Notifications.DTOs;
using VocaNova.API.Features.Notifications.Repositories;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    public const string WordDeletedType = "word_deleted";

    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> NotifyWordDeletedAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        var userIds = await _repository.GetUserIdsAffectedByWordAsync(wordId, cancellationToken);
        if (userIds.Count == 0)
        {
            return 0;
        }

        var wordText = await _repository.GetWordTextAsync(wordId, cancellationToken);
        var displayWord = string.IsNullOrWhiteSpace(wordText) ? $"#{wordId}" : wordText!;
        var now = DateTime.UtcNow;

        // End-user (learner) facing content — stored in Vietnamese; `type`/`ref_id` let the app localize/link if needed.
        const string title = "Từ vựng đã bị gỡ";
        var message = $"Từ \"{displayWord}\" đã bị gỡ khỏi từ điển. Nội dung liên quan trong danh sách của bạn có thể không còn khả dụng.";

        var notifications = userIds
            .Select(userId => new Notification
            {
                UserId = userId,
                Type = WordDeletedType,
                Title = title,
                Message = message,
                RefType = "word",
                RefId = wordId,
                IsRead = false,
                CreatedAt = now,
            })
            .ToList();

        return await _repository.AddRangeAsync(notifications, cancellationToken);
    }

    public async Task<Result<PagedResult<NotificationDto>>> ListAsync(uint userId, NotificationListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<NotificationDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var paged = await _repository.ListByUserAsync(userId, page, query.Limit, query.UnreadOnly, cancellationToken);
        var items = paged.Items.Select(Map).ToList();

        return Result<PagedResult<NotificationDto>>.Ok(
            new PagedResult<NotificationDto>(items, paged.Page, paged.Limit, paged.TotalItems));
    }

    public async Task<Result<int>> UnreadCountAsync(uint userId, CancellationToken cancellationToken = default) =>
        Result<int>.Ok(await _repository.UnreadCountAsync(userId, cancellationToken));

    public async Task<Result<bool>> MarkReadAsync(uint userId, uint notificationId, CancellationToken cancellationToken = default)
    {
        var found = await _repository.MarkReadAsync(userId, notificationId, cancellationToken);
        return found ? Result<bool>.Ok(true) : Result<bool>.NotFound("Notification not found.");
    }

    public async Task<Result<int>> MarkAllReadAsync(uint userId, CancellationToken cancellationToken = default) =>
        Result<int>.Ok(await _repository.MarkAllReadAsync(userId, cancellationToken));

    private static NotificationDto Map(Notification n) => new(
        n.NotificationId,
        n.Type,
        n.Title,
        n.Message,
        n.RefType,
        n.RefId,
        n.IsRead,
        n.CreatedAt,
        n.ReadAt);
}
