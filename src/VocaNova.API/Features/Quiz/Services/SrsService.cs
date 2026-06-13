using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class SrsService : ISrsService
{
    private const float InitialEaseFactor = 2.5f;
    private const float CorrectEaseDelta = 0.1f;
    private const float WrongEaseDelta = 0.32f;
    private const float MinEaseFactor = 1.3f;
    private const int MaxMasteryLevel = 5;
    private const int MasteryStep = 5;

    private readonly ISrsRepository _srsRepository;

    public SrsService(ISrsRepository srsRepository)
    {
        _srsRepository = srsRepository;
    }

    public async Task<Result<UserWordProgressDto>> UpdateProgressAsync(
        uint userId,
        uint wordId,
        bool isCorrect,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<UserWordProgressDto>.Unauthorized("Unauthorized.");
        }

        if (wordId == 0)
        {
            return Result<UserWordProgressDto>.NotFound("Word not found.");
        }

        var progress = await _srsRepository.FindAsync(userId, wordId, cancellationToken);
        if (progress is null)
        {
            progress = CreateInitialProgress(userId, wordId);
            _srsRepository.Add(progress);
        }

        var now = DateTime.UtcNow;
        ApplyResult(progress, isCorrect, now);

        await _srsRepository.SaveChangesAsync(cancellationToken);

        return Result<UserWordProgressDto>.Ok(ToDto(progress));
    }

    private static UserWordProgress CreateInitialProgress(uint userId, uint wordId)
    {
        return new UserWordProgress
        {
            UserId = userId,
            WordId = wordId,
            TestCount = 0,
            CorrectCount = 0,
            WrongCount = 0,
            ConsecutiveCorrect = 0,
            IsInWrongList = false,
            MasteryLevel = 0,
            SrsInterval = 1,
            EaseFactor = InitialEaseFactor,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static void ApplyResult(UserWordProgress progress, bool isCorrect, DateTime now)
    {
        progress.TestCount++;
        progress.LastTestedAt = now;
        progress.UpdatedAt = now;

        if (isCorrect)
        {
            ApplyCorrectResult(progress, now);
            return;
        }

        ApplyWrongResult(progress, now);
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

    private static UserWordProgressDto ToDto(UserWordProgress progress)
    {
        return new UserWordProgressDto(
            progress.ProgressId,
            progress.UserId,
            progress.WordId,
            progress.TestCount,
            progress.CorrectCount,
            progress.WrongCount,
            progress.ConsecutiveCorrect,
            progress.IsInWrongList,
            progress.MasteryLevel,
            progress.SrsInterval,
            progress.EaseFactor,
            progress.LastTestedAt,
            progress.LastWrongAt,
            progress.NextReviewAt,
            progress.UpdatedAt);
    }
}
