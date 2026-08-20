using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Notifications.Contracts.Responses;

public sealed record NotificationResponse(
    [property: JsonPropertyName("notification_id")] uint NotificationId,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("ref_type")] string? RefType,
    [property: JsonPropertyName("ref_id")] uint? RefId,
    [property: JsonPropertyName("is_read")] bool IsRead,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("read_at")] DateTime? ReadAt);
