namespace VocaNova.API.Features.Progress.BLL.Models;

public sealed record WeakestWord(
    uint WordId,
    string Word,
    string? PrimaryMeaning,
    int TestCount,
    int CorrectCount,
    int WrongCount,
    float AccuracyRate,
    int MasteryLevel,
    DateTime? LastWrongAt,
    DateTime? NextReviewAt);

public sealed record WeakestWordStatistics(
    uint WordId,
    string Word,
    string? PrimaryMeaning,
    int TestCount,
    int CorrectCount,
    int WrongCount,
    int MasteryLevel,
    DateTime? LastWrongAt,
    DateTime? NextReviewAt);
