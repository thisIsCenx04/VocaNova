using VocaNova.API.Features.Notifications.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Notifications.BLL.Models;
using VocaNova.API.Features.Notifications.BLL.Services.IServices;

namespace VocaNova.API.Features.Notifications.BLL.Services;

public sealed class NotificationService : INotificationService
{
    public const string WordDeletedType = "word_deleted";
    public const int MaximumPageLimit = 100;

    private const string WordDeletedTitle = "Từ vựng đã bị gỡ";
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotificationListResult> ListAsync(
        uint userId,
        NotificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        if (query.Limit <= 0 || query.Limit > MaximumPageLimit)
        {
            return NotificationListResult.ValidationFailure(
                $"Limit must be between 1 and {MaximumPageLimit}.");
        }

        var deletedWords = await _repository.ListDeletedWordsAsync(
            userId,
            page,
            query.Limit,
            cancellationToken);
        var notifications = deletedWords.Items.Select(Map).ToList();

        return NotificationListResult.Success(
            new PagedCollection<Notification>(
                notifications,
                deletedWords.Page,
                deletedWords.Limit,
                deletedWords.TotalItems));
    }

    private static Notification Map(DeletedWordReference word)
    {
        var displayWord = string.IsNullOrWhiteSpace(word.WordText)
            ? $"#{word.WordId}"
            : word.WordText;
        var message = $"Từ \"{displayWord}\" đã bị gỡ khỏi từ điển. Nội dung liên quan trong danh sách của bạn có thể không còn khả dụng.";

        return new Notification(
            word.WordId,
            WordDeletedType,
            WordDeletedTitle,
            message,
            "word",
            word.WordId,
            false,
            word.DeletedAt,
            null);
    }
}
