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

    Task<WordDetailDto?> FindDailyDetailAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminWordListItemDto>> SearchAdminAsync(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        string? status,
        bool includeDeleted,
        string? wordType,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken = default);

    Task<bool> WordKeyExistsAsync(
        string wordKey,
        uint? excludingWordId = null,
        CancellationToken cancellationToken = default);

    Task<WordDetailDto> CreateAsync(
        CreateWordRequest request,
        string wordKey,
        CancellationToken cancellationToken = default);

    Task<uint?> FindWordIdByKeyAsync(
        string wordKey,
        CancellationToken cancellationToken = default);

    Task<WordDetailDto> CreateWithSenseAsync(
        CreateWordRequest wordRequest,
        string wordKey,
        CreateSenseRequest senseRequest,
        CancellationToken cancellationToken = default);

    Task<WordDetailDto?> UpdateMetadataAsync(
        uint wordId,
        UpdateWordRequest request,
        string wordKey,
        CancellationToken cancellationToken = default);

    Task<bool> SetStatusAsync(
        uint wordId,
        string status,
        CancellationToken cancellationToken = default);

    Task<WordDetailDto?> SetImageUrlAsync(
        uint wordId,
        string? imageUrl,
        CancellationToken cancellationToken = default);

    Task<WordAudioDto?> UpsertAudioAsync(
        uint wordId,
        string accent,
        string storageUrl,
        CancellationToken cancellationToken = default);

    Task<bool> SetAudioStatusAsync(
        uint wordId,
        uint audioId,
        string status,
        CancellationToken cancellationToken = default);

    Task<bool> WordExistsAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<WordSenseDto?> CreateSenseAsync(
        uint wordId,
        CreateSenseRequest request,
        CancellationToken cancellationToken = default);

    Task<WordSenseDto?> CreateNextSenseAsync(
        uint wordId,
        string wordClass,
        string englishDefinition,
        string? vietnameseMeaning,
        CancellationToken cancellationToken = default);

    Task<WordSenseDto?> UpdateSenseAsync(
        uint wordId,
        uint senseId,
        UpdateSenseRequest request,
        CancellationToken cancellationToken = default);
}
