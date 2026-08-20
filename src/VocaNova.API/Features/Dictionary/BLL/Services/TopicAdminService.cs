using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public sealed class TopicAdminService : ITopicAdminService
{
    private readonly ITopicAdminRepository _repository;
    private readonly ITopicCache? _cache;

    public TopicAdminService(ITopicAdminRepository repository, ITopicCache? cache = null)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<DictionaryResult<IReadOnlyCollection<AdminTopic>>> ListAsync(
        AdminTopicQuery query, CancellationToken cancellationToken = default)
    {
        var status = NormalizeStatus(query.Status);
        if (status is not null && status is not (UserStatus.Active or UserStatus.Deleted))
            return DictionaryResult<IReadOnlyCollection<AdminTopic>>.ValidationFailure("Status must be 'active' or 'deleted'.");

        var normalized = query with
        {
            Q = NormalizeNullable(query.Q),
            Status = status,
            IncludeDeleted = query.IncludeDeleted || status == UserStatus.Deleted,
        };
        return DictionaryResult<IReadOnlyCollection<AdminTopic>>.Success(
            await _repository.ListAsync(normalized, cancellationToken));
    }

    public async Task<DictionaryResult<TopicSummary>> CreateAsync(
        CreateTopicCommand command, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(command);
        if (await _repository.NameExistsAsync(normalized.TopicName, null, cancellationToken: cancellationToken))
            return DictionaryResult<TopicSummary>.Conflict("Topic already exists.");
        if (normalized.TopicNameVi is not null
            && await _repository.NameExistsAsync(string.Empty, normalized.TopicNameVi, cancellationToken: cancellationToken))
            return DictionaryResult<TopicSummary>.Conflict("Vietnamese topic name already exists.");

        var ids = normalized.WordIds ?? Array.Empty<uint>();
        if (!await _repository.WordIdsExistAsync(ids, cancellationToken))
            return DictionaryResult<TopicSummary>.ValidationFailure("One or more selected vocabulary words do not exist.");

        var topic = await _repository.CreateAsync(normalized, cancellationToken);
        await InvalidateAsync(topic.TopicId, true, cancellationToken);
        return DictionaryResult<TopicSummary>.Success(topic);
    }

    public async Task<DictionaryResult<int>> AddWordsAsync(
        uint topicId, IReadOnlyCollection<uint>? wordIds, CancellationToken cancellationToken = default)
    {
        if (!await _repository.ExistsAsync(topicId, cancellationToken: cancellationToken))
            return DictionaryResult<int>.NotFound("Topic not found.");

        var ids = wordIds?.Where(id => id > 0).Distinct().ToArray() ?? Array.Empty<uint>();
        if (ids.Length == 0)
            return DictionaryResult<int>.ValidationFailure("Select at least one vocabulary word.");
        if (!await _repository.WordIdsExistAsync(ids, cancellationToken))
            return DictionaryResult<int>.ValidationFailure("One or more selected vocabulary words do not exist.");

        var added = await _repository.AddWordsAsync(topicId, ids, cancellationToken);
        if (added == 0)
            return DictionaryResult<int>.Conflict("The selected vocabulary is already in this topic.");

        await InvalidateAsync(topicId, true, cancellationToken);
        return DictionaryResult<int>.Success(added);
    }

    public async Task<DictionaryResult<TopicSummary>> UpdateAsync(
        uint topicId, UpdateTopicCommand command, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(command);
        if (await _repository.NameExistsAsync(normalized.TopicName, null, topicId, cancellationToken))
            return DictionaryResult<TopicSummary>.Conflict("Topic already exists.");
        if (normalized.TopicNameVi is not null
            && await _repository.NameExistsAsync(string.Empty, normalized.TopicNameVi, topicId, cancellationToken))
            return DictionaryResult<TopicSummary>.Conflict("Vietnamese topic name already exists.");
        if (normalized.WordIds is not null
            && !await _repository.WordIdsExistAsync(normalized.WordIds, cancellationToken))
            return DictionaryResult<TopicSummary>.ValidationFailure("One or more selected vocabulary words do not exist.");

        var topic = await _repository.UpdateAsync(topicId, normalized, cancellationToken);
        if (topic is null) return DictionaryResult<TopicSummary>.NotFound("Topic not found.");
        await InvalidateAsync(topicId, true, cancellationToken);
        return DictionaryResult<TopicSummary>.Success(topic);
    }

    public Task<DictionaryResult<bool>> SoftDeleteAsync(uint topicId, CancellationToken cancellationToken = default) =>
        SetStatusAsync(topicId, UserStatus.Deleted, cancellationToken);

    public Task<DictionaryResult<bool>> RestoreAsync(uint topicId, CancellationToken cancellationToken = default) =>
        SetStatusAsync(topicId, UserStatus.Active, cancellationToken);

    private async Task<DictionaryResult<bool>> SetStatusAsync(
        uint topicId, string status, CancellationToken cancellationToken)
    {
        if (!await _repository.SetStatusAsync(topicId, status, cancellationToken))
            return DictionaryResult<bool>.NotFound("Topic not found.");
        await InvalidateAsync(topicId, false, cancellationToken);
        return DictionaryResult<bool>.Success(true);
    }

    private async Task InvalidateAsync(uint topicId, bool includeWords, CancellationToken cancellationToken)
    {
        if (_cache is null) return;
        await _cache.RemoveTopicsAsync(cancellationToken);
        if (includeWords) await _cache.RemoveTopicWordsAsync(topicId, cancellationToken);
    }

    private static CreateTopicCommand Normalize(CreateTopicCommand command) => command with
    {
        TopicName = command.TopicName.Trim(), TopicNameVi = NormalizeNullable(command.TopicNameVi),
        Icon = NormalizeNullable(command.Icon), WordIds = command.WordIds?.Distinct().ToArray(),
    };

    private static UpdateTopicCommand Normalize(UpdateTopicCommand command) => command with
    {
        TopicName = command.TopicName.Trim(), TopicNameVi = NormalizeNullable(command.TopicNameVi),
        Icon = NormalizeNullable(command.Icon), WordIds = command.WordIds?.Distinct().ToArray(),
    };

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeStatus(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
