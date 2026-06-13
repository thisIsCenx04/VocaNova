using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public sealed class QuizQuestionRepository : IQuizQuestionRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public QuizQuestionRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QuizQuestionWordDto?> FindQuestionWordAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .AsNoTracking()
            .Include(word => word.WordSenses)
            .Include(word => word.WordTopics)
            .Where(word => word.WordId == wordId && word.Status == UserStatus.Active)
            .SingleOrDefaultAsync(cancellationToken);

        return word is null ? null : MapToQuestionWord(word);
    }

    public async Task<IReadOnlyCollection<QuizQuestionWordDto>> GetDistractorCandidatesAsync(
        uint excludingWordId,
        string wordClass,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Words
            .AsNoTracking()
            .Include(word => word.WordSenses)
            .Include(word => word.WordTopics)
            .Where(word => word.WordId != excludingWordId
                && word.Status == UserStatus.Active
                && word.WordSenses.Any(sense => sense.WordClass == wordClass));

        if (topicIds.Count > 0)
        {
            query = _dbContext.Words
                .AsNoTracking()
                .Include(word => word.WordSenses)
                .Include(word => word.WordTopics)
                .Where(word => word.WordId != excludingWordId
                    && word.Status == UserStatus.Active
                    && (word.WordSenses.Any(sense => sense.WordClass == wordClass)
                        || word.WordTopics.Any(wordTopic => topicIds.Contains(wordTopic.TopicId))));
        }

        var words = await query.ToListAsync(cancellationToken);

        return words
            .Select(word => MapToQuestionWord(word, wordClass))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .ToArray();
    }

    private static QuizQuestionWordDto? MapToQuestionWord(Word word, string? preferredWordClass = null)
    {
        var sense = word.WordSenses
            .Where(sense => preferredWordClass is null || sense.WordClass == preferredWordClass)
            .OrderBy(sense => sense.SenseOrder)
            .ThenBy(sense => sense.SenseId)
            .FirstOrDefault()
            ?? word.WordSenses
                .OrderBy(sense => sense.SenseOrder)
                .ThenBy(sense => sense.SenseId)
                .FirstOrDefault();

        if (sense is null)
        {
            return null;
        }

        return new QuizQuestionWordDto(
            word.WordId,
            word.Word1,
            sense.SenseId,
            sense.WordClass,
            sense.EnglishDefinition,
            sense.VietnameseMeaning,
            word.WordTopics
                .Select(wordTopic => wordTopic.TopicId)
                .ToArray());
    }
}
