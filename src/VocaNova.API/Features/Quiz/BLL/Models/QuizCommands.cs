namespace VocaNova.API.Features.Quiz.BLL.Models;

public sealed record BuildQuizPoolCommand(
    string ScopeType,
    DateOnly? ScopeDateFrom,
    DateOnly? ScopeDateTo,
    IReadOnlyCollection<uint>? TopicIds,
    string WordOrder,
    int? WordLimit,
    string AnswerMethod,
    uint? ListId = null);

public sealed record CreateQuizSessionCommand(
    string? Mode,
    int QuestionType,
    string? ScopeType,
    DateOnly? ScopeDateFrom,
    DateOnly? ScopeDateTo,
    IReadOnlyCollection<uint>? TopicIds,
    string? WordOrder,
    int? WordLimit,
    int? TimeLimitSec,
    int? Lives,
    string? AnswerMethod,
    uint? ListId = null);

public sealed record SubmitAnswerCommand(
    uint WordId,
    string? UserAnswer,
    uint? ListId = null);

public sealed record QuizHistoryQuery(int Page, int Limit);

public sealed record WrongWordsQuery(int Page, int Limit);
