using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.BLL.Models;

public sealed record AnswerGrade(bool IsCorrect);

public sealed record QuizQuestion(
    uint WordId,
    uint SenseId,
    int QuestionType,
    string DisplayContent,
    string ExpectedAnswer,
    IReadOnlyCollection<string> Choices);

public sealed record QuizSession(
    uint SessionId,
    string AnswerMethod,
    string Mode,
    int QuestionType,
    string ScopeType,
    DateOnly? ScopeDateFrom,
    DateOnly? ScopeDateTo,
    string WordOrder,
    int? WordLimit,
    int? TimeLimitSec,
    int? Lives,
    int QuestionCount,
    string Status,
    DateTime StartedAt,
    IReadOnlyCollection<uint> TopicIds,
    uint? ListId = null);

public sealed record CreatedQuizSession(QuizSession Session, QuizQuestion FirstQuestion);

public sealed record QuizAnswer(
    uint SessionId,
    uint WordId,
    bool IsCorrect,
    string ExpectedAnswer,
    int CorrectCount,
    int WrongCount,
    float Score,
    float? AiScore,
    string? AiExplanation,
    string? AiSuggestion,
    QuizQuestion? NextQuestion);

public sealed record TestAnswerResult(
    uint AnswerId,
    uint WordId,
    uint? SenseId,
    int QuestionNumber,
    int QuestionType,
    string DisplayContent,
    string ExpectedAnswer,
    string? UserAnswer,
    bool? IsCorrect,
    float? AiScore,
    string? AiExplanation,
    string? AiSuggestion);

public sealed record QuizResult(
    uint SessionId,
    string Status,
    int CorrectCount,
    int WrongCount,
    int QuestionCount,
    int AnsweredCount,
    float Accuracy,
    int? DurationSec,
    int MaxStreak,
    float Score,
    DateTime StartedAt,
    DateTime? EndedAt,
    IReadOnlyCollection<TestAnswerResult> Answers);

public sealed record QuizHistoryItem(
    uint SessionId,
    string AnswerMethod,
    string Mode,
    int QuestionType,
    int QuestionCount,
    int CorrectCount,
    int WrongCount,
    float Accuracy,
    float Score,
    int MaxStreak,
    string Status,
    DateTime StartedAt,
    DateTime? EndedAt);

public sealed record WrongWord(
    uint WordId,
    string Word,
    string? PrimaryMeaning,
    int TestCount,
    int CorrectCount,
    int WrongCount,
    int MasteryLevel,
    DateTime? LastWrongAt,
    DateTime? NextReviewAt);
