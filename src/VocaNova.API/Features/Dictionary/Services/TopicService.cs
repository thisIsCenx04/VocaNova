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

    public async Task<Result<IReadOnlyCollection<AdminTopicDto>>> GetAdminTopicsAsync(
        AdminTopicQuery query,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim().ToLowerInvariant();
        if (status is not null && status != UserStatus.Active && status != UserStatus.Deleted)
        {
            return Result<IReadOnlyCollection<AdminTopicDto>>.Fail("Status must be 'active' or 'deleted'.");
        }

        // Lọc status='deleted' chỉ có nghĩa khi đã bỏ global filter.
        var normalized = new AdminTopicQuery
        {
            Q = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim(),
            Status = status,
            IncludeDeleted = query.IncludeDeleted || status == UserStatus.Deleted,
        };

        var topics = await _topicRepository.GetAdminTopicsAsync(normalized, cancellationToken);
        return Result<IReadOnlyCollection<AdminTopicDto>>.Ok(topics);
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

    public async Task<Result<TopicSummaryDto>> CreateAsync(
        CreateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeCreateTopicRequest(request);
        if (await _topicRepository.TopicNameExistsAsync(normalizedRequest.TopicName!, cancellationToken: cancellationToken))
        {
            return Result<TopicSummaryDto>.Conflict("Topic already exists.");
        }

        var topic = await _topicRepository.CreateAsync(normalizedRequest, cancellationToken);
        await RemoveTopicsCacheAsync(cancellationToken);

        return Result<TopicSummaryDto>.Ok(topic);
    }

    public async Task<Result<TopicSummaryDto>> UpdateAsync(
        uint topicId,
        UpdateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeUpdateTopicRequest(request);
        if (await _topicRepository.TopicNameExistsAsync(normalizedRequest.TopicName!, topicId, cancellationToken))
        {
            return Result<TopicSummaryDto>.Conflict("Topic already exists.");
        }

        var topic = await _topicRepository.UpdateAsync(topicId, normalizedRequest, cancellationToken);
        if (topic is null)
        {
            return Result<TopicSummaryDto>.NotFound("Topic not found.");
        }

        await RemoveTopicsCacheAsync(cancellationToken);

        return Result<TopicSummaryDto>.Ok(topic);
    }

    public async Task<Result<bool>> SoftDeleteAsync(
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        if (await _topicRepository.HasActiveWordsAsync(topicId, cancellationToken))
        {
            return Result<bool>.Conflict("Topic still has active words.");
        }

        var deleted = await _topicRepository.SetStatusAsync(topicId, UserStatus.Deleted, cancellationToken);
        if (!deleted)
        {
            return Result<bool>.NotFound("Topic not found.");
        }

        await RemoveTopicsCacheAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RestoreAsync(
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        var restored = await _topicRepository.SetStatusAsync(topicId, UserStatus.Active, cancellationToken);
        if (!restored)
        {
            return Result<bool>.NotFound("Topic not found.");
        }

        await RemoveTopicsCacheAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }

    private async Task RemoveTopicsCacheAsync(CancellationToken cancellationToken)
    {
        if (_topicCache is not null)
        {
            await _topicCache.RemoveTopicsAsync(cancellationToken);
        }
    }

    private static CreateTopicRequest NormalizeCreateTopicRequest(CreateTopicRequest request)
    {
        return request with
        {
            TopicName = request.TopicName!.Trim(),
            TopicNameVi = NormalizeNullable(request.TopicNameVi),
            Icon = NormalizeNullable(request.Icon),
        };
    }

    private static UpdateTopicRequest NormalizeUpdateTopicRequest(UpdateTopicRequest request)
    {
        return request with
        {
            TopicName = request.TopicName!.Trim(),
            TopicNameVi = NormalizeNullable(request.TopicNameVi),
            Icon = NormalizeNullable(request.Icon),
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
