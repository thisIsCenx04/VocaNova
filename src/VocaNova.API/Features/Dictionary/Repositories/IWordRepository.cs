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

    Task<WordDetailDto?> FindDetailAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<bool> WordKeyExistsAsync(
        string wordKey,
        uint? excludingWordId = null,
        CancellationToken cancellationToken = default);

    Task<WordDetailDto> CreateAsync(
        CreateWordRequest request,
        string wordKey,
        CancellationToken cancellationToken = default);

    Task<WordDetailDto?> UpdateMetadataAsync(
        uint wordId,
        UpdateWordRequest request,
        string wordKey,
        CancellationToken cancellationToken = default);
}
