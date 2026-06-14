namespace VocaNova.API.Features.Knn.DTOs;

public sealed record KnnTopicPreferenceDto(
    uint UserId,
    uint TopicId,
    string TopicName,
    string? TopicNameVi,
    string? Icon,
    string Source,
    string Status,
    DateTime CreatedAt);
