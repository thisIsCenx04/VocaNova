namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record BuildQuizPoolRequest(
    string ScopeType,
    DateOnly? ScopeDateFrom,
    DateOnly? ScopeDateTo,
    IReadOnlyCollection<uint>? TopicIds,
    string WordOrder,
    int? WordLimit,
    string AnswerMethod,
    uint? ListId = null);
