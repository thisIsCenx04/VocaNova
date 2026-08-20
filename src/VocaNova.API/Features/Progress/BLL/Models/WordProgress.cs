namespace VocaNova.API.Features.Progress.BLL.Models;

public sealed record WordProgress(
    uint WordId,
    string Word,
    string? PrimaryMeaning,
    int TestCount,
    int CorrectCount,
    int WrongCount,
    float AccuracyRate,
    int ConsecutiveCorrect,
    bool IsInWrongList,
    int MasteryLevel,
    int SrsInterval,
    float EaseFactor,
    DateTime? LastTestedAt,
    DateTime? LastWrongAt,
    DateTime? NextReviewAt,
    DateTime UpdatedAt);

public sealed record WordProgressStatistics(
    uint WordId,
    string Word,
    string? PrimaryMeaning,
    int TestCount,
    int CorrectCount,
    int WrongCount,
    int ConsecutiveCorrect,
    bool IsInWrongList,
    int MasteryLevel,
    int SrsInterval,
    float EaseFactor,
    DateTime? LastTestedAt,
    DateTime? LastWrongAt,
    DateTime? NextReviewAt,
    DateTime UpdatedAt);
