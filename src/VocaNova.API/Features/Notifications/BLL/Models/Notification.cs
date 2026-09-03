namespace VocaNova.API.Features.Notifications.BLL.Models;

public sealed record Notification(
    uint NotificationId,
    string Type,
    string Title,
    string Message,
    string? ReferenceType,
    uint? ReferenceId,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);
