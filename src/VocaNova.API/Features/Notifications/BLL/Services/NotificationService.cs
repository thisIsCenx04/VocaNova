using VocaNova.API.Features.Notifications.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Notifications.BLL.Models;
using VocaNova.API.Features.Notifications.BLL.Services.IServices;

namespace VocaNova.API.Features.Notifications.BLL.Services;

public sealed class NotificationService : INotificationService
{
    public const string WordDeletedType = "word_deleted";
    public const string WrongListMasteredType = "wrong_list_mastered";
    public const int MaximumPageLimit = 100;

    private const uint WrongListMasteredIdOffset = 2_000_000_000;
    private const string WordDeletedTitle = "Vocabulary was removed";
    private const string WrongListMasteredTitle = "Great progress!";
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

        var fetchLimit = page * query.Limit;
        var deletedWords = await _repository.ListDeletedWordsAsync(
            userId,
            1,
            fetchLimit,
            cancellationToken);
        var masteredWrongWords = await _repository.ListMasteredWrongWordsAsync(
            userId,
            1,
            fetchLimit,
            cancellationToken);
        var notifications = deletedWords.Items.Select(Map)
            .Concat(masteredWrongWords.Items.Select(Map))
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.NotificationId)
            .Skip((page - 1) * query.Limit)
            .Take(query.Limit)
            .ToList();

        return NotificationListResult.Success(
            new PagedCollection<Notification>(
                notifications,
                page,
                query.Limit,
                deletedWords.TotalItems + masteredWrongWords.TotalItems));
    }

    private static Notification Map(DeletedWordReference word)
    {
        var displayWord = string.IsNullOrWhiteSpace(word.WordText)
            ? $"#{word.WordId}"
            : word.WordText;
        var message = $"\"{displayWord}\" was removed from the dictionary. Related content in your lists may no longer be available.";

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

    private static Notification Map(MasteredWrongWordReference word)
    {
        var displayWord = string.IsNullOrWhiteSpace(word.WordText)
            ? $"#{word.WordId}"
            : word.WordText;
        var message = $"Nice work! \"{displayWord}\" reached level 5 and was removed from your recently wrong words.";

        return new Notification(
            WrongListMasteredIdOffset + (word.ProgressId % WrongListMasteredIdOffset),
            WrongListMasteredType,
            WrongListMasteredTitle,
            message,
            "word",
            word.WordId,
            false,
            word.MasteredAt,
            null);
    }
}
