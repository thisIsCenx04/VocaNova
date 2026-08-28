using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordReadRepository
{
    Task<PagedCollection<WordSummary>> SearchAsync(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        bool? isPhrase,
        CancellationToken cancellationToken = default);

    Task<WordDetail?> FindDetailAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetDailyCandidateWordIdsAsync(
        bool requirePlayableAudio,
        CancellationToken cancellationToken = default);
}
