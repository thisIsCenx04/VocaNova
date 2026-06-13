using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.AiGrading.Repositories;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Services;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.AiGrading.Services;

public sealed class CachedAiGradingService : IAiGradingService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    private readonly IAiGradingCacheRepository _aiGradingCacheRepository;
    private readonly IAiGradingProvider _aiGradingProvider;

    public CachedAiGradingService(
        IAiGradingCacheRepository aiGradingCacheRepository,
        IAiGradingProvider aiGradingProvider)
    {
        _aiGradingCacheRepository = aiGradingCacheRepository;
        _aiGradingProvider = aiGradingProvider;
    }

    public async Task<AiGradingResult> GradeAsync(
        uint wordId,
        int questionType,
        string? userAnswer,
        string expectedAnswer,
        CancellationToken cancellationToken = default)
    {
        var normalizedAnswer = (userAnswer ?? string.Empty).NormalizeAnswer();
        var cacheKey = CreateCacheKey(wordId, questionType, normalizedAnswer);
        var cached = await _aiGradingCacheRepository.FindValidAndIncrementHitAsync(
            cacheKey,
            DateTime.UtcNow,
            cancellationToken);
        if (cached is not null)
        {
            return new AiGradingResult(
                cached.Score >= AppSettings.AiPassThreshold,
                cached.Score,
                cached.Explanation ?? string.Empty,
                cached.Suggestion);
        }

        var result = await _aiGradingProvider.GradeAsync(
            wordId,
            questionType,
            userAnswer,
            expectedAnswer,
            cancellationToken);

        var now = DateTime.UtcNow;
        await _aiGradingCacheRepository.SaveAsync(
            new AiGradingCache
            {
                CacheKey = cacheKey,
                WordId = wordId,
                QuestionType = questionType,
                UserAnswerNormalized = normalizedAnswer,
                ExpectedAnswer = expectedAnswer,
                AiScore = result.Score,
                AiExplanation = result.Explanation,
                AiSuggestion = result.Suggestion,
                HitCount = 1,
                CreatedAt = now,
                ExpiresAt = now.Add(CacheTtl),
            },
            cancellationToken);

        return result;
    }

    public static string CreateCacheKey(
        uint wordId,
        int questionType,
        string normalizedUserAnswer)
    {
        return TokenHelper.HashSha256($"{wordId}:{questionType}:{normalizedUserAnswer}");
    }
}
