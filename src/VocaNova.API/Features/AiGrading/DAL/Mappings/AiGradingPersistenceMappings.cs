using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.AiGrading.DAL.Mappings;

public static class AiGradingPersistenceMappings
{
    public static CachedAiGrade ToBusinessModel(this AiGradingCache entity) =>
        new(entity.AiScore, entity.AiExplanation, entity.AiSuggestion,
            new AiGradeCacheKey(entity.CacheKey, entity.WordId, entity.QuestionType,
                entity.UserAnswerNormalized, entity.ExpectedAnswer),
            entity.HitCount, entity.CreatedAt, entity.ExpiresAt);
}
