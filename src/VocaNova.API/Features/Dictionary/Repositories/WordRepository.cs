using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Dictionary.Repositories;

public sealed class WordRepository : IWordRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public WordRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<WordSummaryDto>> SearchAsync(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        bool? isPhrase,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Words
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            query = query.Where(word => EF.Functions.Like(word.WordKey, normalizedQuery + "%"));
        }

        if (!string.IsNullOrWhiteSpace(cefr))
        {
            query = query.Where(word => word.CefrLevel == cefr);
        }

        if (topicId.HasValue)
        {
            query = query.Where(word => word.WordTopics.Any(wordTopic => wordTopic.TopicId == topicId.Value));
        }

        if (isPhrase.HasValue)
        {
            query = query.Where(word => word.IsPhrase == isPhrase.Value);
        }

        return query
            .OrderBy(word => word.WordKey)
            .ThenBy(word => word.WordId)
            .Select(word => new WordSummaryDto(
                word.WordId,
                word.Word1,
                word.PhoneticUs ?? word.PhoneticUk,
                word.CefrLevel,
                word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                word.ImageUrl))
            .ToPagedResultAsync(page, limit, cancellationToken);
    }
}
