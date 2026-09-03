namespace VocaNova.API.Features.Knn.BLL.Models;

public sealed record KnnTopicAnswerStatistics(uint TopicId, int CorrectCount, int TotalCount);

public sealed record KnnNeighbor(uint UserId, double Similarity);

public sealed record KnnMasteredWord(uint UserId, uint WordId);

public sealed record KnnNeighborWord(uint UserId, uint WordId);

public sealed record WordRecommendationItem(
    uint WordId,
    string Word,
    string? PhoneticUk,
    string? PrimaryMeaning,
    string? ImageUrl,
    string? CefrLevel,
    double Score);
