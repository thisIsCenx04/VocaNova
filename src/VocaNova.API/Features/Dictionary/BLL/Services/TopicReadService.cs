using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.BLL.Services.IServices;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public sealed class TopicReadService : ITopicReadService
{
    private const int MaximumPageLimit = 100;

    private readonly ITopicReadRepository _topicRepository;
    private readonly IWordReadRepository _wordRepository;
    private readonly ITopicCache? _topicCache;

    public TopicReadService(
        ITopicReadRepository topicRepository,
        IWordReadRepository wordRepository,
        ITopicCache? topicCache = null)
    {
        _topicRepository = topicRepository;
        _wordRepository = wordRepository;
        _topicCache = topicCache;
    }

    public async Task<DictionaryResult<IReadOnlyCollection<TopicSummary>>> GetTopicsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_topicCache is not null)
        {
            var cached = await _topicCache.GetTopicsAsync(cancellationToken);
            if (cached is not null)
            {
                return DictionaryResult<IReadOnlyCollection<TopicSummary>>.Success(cached);
            }
        }

        var topics = await _topicRepository.GetTopicsAsync(cancellationToken);
        if (_topicCache is not null)
        {
            await _topicCache.SetTopicsAsync(topics, cancellationToken);
        }

        return DictionaryResult<IReadOnlyCollection<TopicSummary>>.Success(topics);
    }

    public async Task<DictionaryResult<PagedCollection<WordSummary>>> GetWordsAsync(
        uint topicId,
        TopicWordsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return DictionaryResult<PagedCollection<WordSummary>>.ValidationFailure(
                "Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > MaximumPageLimit)
        {
            return DictionaryResult<PagedCollection<WordSummary>>.ValidationFailure(
                $"Limit must be between 1 and {MaximumPageLimit}.");
        }

        if (!await _topicRepository.ExistsAsync(topicId, cancellationToken))
        {
            return DictionaryResult<PagedCollection<WordSummary>>.NotFound("Topic not found.");
        }

        if (_topicCache is not null)
        {
            var cached = await _topicCache.GetTopicWordsAsync(
                topicId,
                query.Page,
                query.Limit,
                cancellationToken);
            if (cached is not null)
            {
                return DictionaryResult<PagedCollection<WordSummary>>.Success(cached);
            }
        }

        var words = await _wordRepository.SearchAsync(
            normalizedQuery: null,
            query.Page,
            query.Limit,
            cefr: null,
            topicId,
            isPhrase: null,
            cancellationToken);

        if (_topicCache is not null)
        {
            await _topicCache.SetTopicWordsAsync(
                topicId,
                query.Page,
                query.Limit,
                words,
                cancellationToken);
        }

        return DictionaryResult<PagedCollection<WordSummary>>.Success(words);
    }
}
