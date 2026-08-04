namespace VocaNova.API.Features.Knn.DTOs;

public sealed record KnnTopicAnswerStatsDto(uint TopicId, int CorrectCount, int TotalCount);
