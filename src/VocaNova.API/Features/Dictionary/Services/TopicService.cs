using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Dictionary.Services;

public sealed class TopicService : ITopicService
{
    private readonly ITopicRepository _topicRepository;
    private readonly IWordRepository _wordRepository;
    private readonly ITopicCache? _topicCache;

    public TopicService(
        ITopicRepository topicRepository,
        IWordRepository wordRepository,
        ITopicCache? topicCache = null)
    {
        _topicRepository = topicRepository;
        _wordRepository = wordRepository;
        _topicCache = topicCache;
    }

    public async Task<Result<IReadOnlyCollection<TopicSummaryDto>>> GetTopicsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_topicCache is not null)
        {
            var cached = await _topicCache.GetTopicsAsync(cancellationToken);
            if (cached is not null)
            {
                return Result<IReadOnlyCollection<TopicSummaryDto>>.Ok(cached);
            }
        }

        var topics = await _topicRepository.GetTopicsAsync(cancellationToken);

        if (_topicCache is not null)
        {
            await _topicCache.SetTopicsAsync(topics, cancellationToken);
        }

        return Result<IReadOnlyCollection<TopicSummaryDto>>.Ok(topics);
    }

    public async Task<Result<PagedResult<WordSummaryDto>>> GetWordsAsync(
        uint topicId,
        TopicWordsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result<PagedResult<WordSummaryDto>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<WordSummaryDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        if (!await _topicRepository.ExistsAsync(topicId, cancellationToken))
        {
            return Result<PagedResult<WordSummaryDto>>.NotFound("Topic not found.");
        }

        if (_topicCache is not null)
        {
            var cached = await _topicCache.GetTopicWordsAsync(topicId, query.Page, query.Limit, cancellationToken);
            if (cached is not null)
            {
                return Result<PagedResult<WordSummaryDto>>.Ok(cached);
            }
        }

        var result = await _wordRepository.SearchAsync(
            normalizedQuery: null,
            page: query.Page,
            limit: query.Limit,
            cefr: null,
            topicId: topicId,
            isPhrase: null,
            cancellationToken);

        if (_topicCache is not null)
        {
            await _topicCache.SetTopicWordsAsync(topicId, query.Page, query.Limit, result, cancellationToken);
        }

        return Result<PagedResult<WordSummaryDto>>.Ok(result);
    }
}
