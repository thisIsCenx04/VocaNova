using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public sealed class SrsService : ISrsService
{
    private const float InitialEaseFactor = 2.5f;
    private const float CorrectEaseDelta = 0.1f;
    private const float WrongEaseDelta = 0.32f;
    private const float MinEaseFactor = 1.3f;
    private const int MaxMasteryLevel = 5;
    private const int MasteryStep = 5;

    private readonly ISrsRepository _repository;
    public SrsService(ISrsRepository repository) => _repository = repository;

    public async Task<QuizOperationResult<UserWordProgress>> UpdateProgressAsync(
        uint userId, uint wordId, bool isCorrect, CancellationToken cancellationToken = default)
    {
        if (userId == 0) return QuizOperationResult<UserWordProgress>.Unauthorized("Unauthorized.");
        if (wordId == 0) return QuizOperationResult<UserWordProgress>.NotFound("Word not found.");

        var progress = await _repository.FindAsync(userId, wordId, cancellationToken)
            ?? CreateInitialProgress(userId, wordId);
        var now = DateTime.UtcNow;
        ApplyResult(progress, isCorrect, now);
        _repository.Stage(progress);
        return QuizOperationResult<UserWordProgress>.Success(progress);
    }

    private static UserWordProgress CreateInitialProgress(uint userId, uint wordId) => new()
    {
        UserId = userId,
        WordId = wordId,
        SrsInterval = 1,
        EaseFactor = InitialEaseFactor,
        UpdatedAt = DateTime.UtcNow,
    };

    private static void ApplyResult(UserWordProgress progress, bool isCorrect, DateTime now)
    {
        progress.TestCount++;
        progress.LastTestedAt = now;
        progress.UpdatedAt = now;
        if (isCorrect) ApplyCorrectResult(progress, now); else ApplyWrongResult(progress, now);
    }

    private static void ApplyCorrectResult(UserWordProgress progress, DateTime now)
    {
        progress.CorrectCount++;
        progress.ConsecutiveCorrect++;
        progress.IsInWrongList = false;
        progress.EaseFactor = Math.Max(MinEaseFactor, progress.EaseFactor + CorrectEaseDelta);
        progress.SrsInterval = progress.ConsecutiveCorrect switch
        {
            <= 1 => 1,
            2 => 6,
            _ => Math.Max(1, (int)MathF.Ceiling(progress.SrsInterval * progress.EaseFactor)),
        };
        if (progress.ConsecutiveCorrect >= MasteryStep
            && progress.ConsecutiveCorrect % MasteryStep == 0
            && progress.MasteryLevel < MaxMasteryLevel)
        {
            progress.MasteryLevel++;
        }
        progress.NextReviewAt = now.AddDays(progress.SrsInterval);
    }

    private static void ApplyWrongResult(UserWordProgress progress, DateTime now)
    {
        progress.WrongCount++;
        progress.ConsecutiveCorrect = 0;
        progress.IsInWrongList = true;
        progress.SrsInterval = 1;
        progress.EaseFactor = Math.Max(MinEaseFactor, progress.EaseFactor - WrongEaseDelta);
        progress.LastWrongAt = now;
        progress.NextReviewAt = now.AddDays(progress.SrsInterval);
    }
}
