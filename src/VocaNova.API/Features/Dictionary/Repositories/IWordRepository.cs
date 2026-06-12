using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Repositories;

public interface IWordRepository
{
    Task<PagedResult<WordSummaryDto>> SearchAsync(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        bool? isPhrase,
        CancellationToken cancellationToken = default);
}
