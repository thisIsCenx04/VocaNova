namespace VocaNova.API.Features.Quiz.BLL.Models;

public sealed record QuizPoolWord(
    uint WordId,
    DateTime AddedAt,
    int WrongCount = 0);

public sealed record QuizQuestionWord(
    uint WordId,
    string Word,
    uint SenseId,
    string WordClass,
    string EnglishDefinition,
    string? VietnameseMeaning,
    IReadOnlyCollection<uint> TopicIds);

public sealed class UserWordProgress
{
    public uint ProgressId { get; set; }
    public uint UserId { get; init; }
    public uint WordId { get; init; }
    public int TestCount { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int ConsecutiveCorrect { get; set; }
    public bool IsInWrongList { get; set; }
    public int MasteryLevel { get; set; }
    public int SrsInterval { get; set; }
    public float EaseFactor { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public DateTime? LastWrongAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class QuizSubmissionState
{
    public uint SessionId { get; init; }
    public uint UserId { get; init; }
    public string AnswerMethod { get; init; } = string.Empty;
    public int QuestionType { get; init; }
    public string ScopeType { get; init; } = string.Empty;
    public DateOnly? ScopeDateFrom { get; init; }
    public DateOnly? ScopeDateTo { get; init; }
    public string WordOrder { get; init; } = string.Empty;
    public int QuestionCount { get; init; }
    public string Status { get; set; } = string.Empty;
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public float Score { get; set; }
    public int MaxStreak { get; set; }
    public DateTime? EndedAt { get; set; }
    public IReadOnlyCollection<uint> TopicIds { get; init; } = Array.Empty<uint>();
    public List<QuizSubmissionAnswer> Answers { get; init; } = [];
}

public sealed class QuizSubmissionAnswer
{
    public uint AnswerId { get; set; }
    public uint WordId { get; init; }
    public uint? SenseId { get; set; }
    public int QuestionNumber { get; init; }
    public int QuestionType { get; set; }
    public string DisplayContent { get; set; } = string.Empty;
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string? AcceptedAnswersJson { get; init; }
    public string? UserAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public float? AiScore { get; set; }
    public string? AiExplanation { get; set; }
    public string? AiSuggestion { get; set; }
}

public sealed record QuizSubmissionChanges(
    QuizSubmissionState Session,
    QuizSubmissionAnswer Answer,
    UserWordProgress Progress);

public sealed class QuizResultState
{
    public uint SessionId { get; init; }
    public string Status { get; set; } = string.Empty;
    public int QuestionCount { get; init; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public float Score { get; set; }
    public int MaxStreak { get; set; }
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; set; }
    public IReadOnlyCollection<TestAnswerResult> Answers { get; init; } = Array.Empty<TestAnswerResult>();
}

public sealed record QuizFinishChanges(QuizResultState Session);
