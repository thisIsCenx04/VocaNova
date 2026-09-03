namespace VocaNova.API.Features.Knn.BLL.Models;

public sealed record TopicRecommendation(
    uint TopicId,
    string TopicName,
    string? TopicNameVi,
    string? Icon,
    int WordCount,
    double RecommendationScore);

public sealed record PersonalTopicRecommendationWord(
    uint WordId,
    string Word,
    string? Phonetic,
    string? Cefr,
    string? PrimaryMeaning);

public sealed record PersonalTopicRecommendation(
    uint TopicId,
    string Name,
    string? NameVi,
    string? Icon,
    int WordCount,
    double RecommendationScore,
    IReadOnlyCollection<PersonalTopicRecommendationWord> Words);

public sealed record NeighborPersonalTopic(
    uint OwnerUserId,
    uint TopicId,
    string Name,
    string? NameVi,
    string? Icon,
    int WordCount,
    IReadOnlyCollection<PersonalTopicRecommendationWord> Words);

public sealed record WordRecommendation(
    uint WordId,
    string Word,
    string? PhoneticUk,
    string? PrimaryMeaning,
    string? ImageUrl,
    string? CefrLevel,
    double Score);

public sealed record KnnRebuildStatus(DateTime? LastRebuildAt, bool IsRunning);
