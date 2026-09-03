using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.AiGrading.BLL.Abstractions;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.BLL.Services.IServices;

namespace VocaNova.API.Features.AiGrading.BLL.Services;

public sealed class CachedAiGradingService : IAiGradingService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);
    private readonly IAiGradingCacheRepository _repository;
    private readonly IAiGradingProvider _provider;

    public CachedAiGradingService(IAiGradingCacheRepository repository, IAiGradingProvider provider)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<AiGrade> GradeAsync(uint wordId, int questionType, string? userAnswer,
        string expectedAnswer, CancellationToken cancellationToken = default)
    {
        var normalized = (userAnswer ?? string.Empty).NormalizeAnswer();
        var key = new AiGradeCacheKey(CreateCacheKey(wordId, questionType, normalized),
            wordId, questionType, normalized, expectedAnswer);
        var cached = await _repository.FindValidAndRecordHitAsync(key, DateTime.UtcNow, cancellationToken);
        if (cached is not null)
        {
            return new AiGrade(cached.Score >= AppSettings.AiPassThreshold, cached.Score,
                cached.Explanation ?? string.Empty, cached.Suggestion);
        }

        var result = await _provider.GradeAsync(
            new AiGradeRequest(wordId, questionType, userAnswer, expectedAnswer), cancellationToken);
        if (!result.FromAi) return result;

        var now = DateTime.UtcNow;
        await _repository.SaveAsync(new CachedAiGrade(result.Score, result.Explanation,
            result.Suggestion, key, 1, now, now.Add(CacheTtl)), cancellationToken);
        return result;
    }

    public static string CreateCacheKey(uint wordId, int questionType, string normalizedUserAnswer) =>
        TokenHelper.HashSha256($"{wordId}:{questionType}:{normalizedUserAnswer}");
}
