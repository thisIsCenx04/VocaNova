using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Services;

public interface IWordService
{
    Task<Result<PagedResult<WordSummaryDto>>> SearchAsync(
        WordSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<WordDetailDto>> GetByIdAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<Result<WordDetailDto>> CreateAsync(
        CreateWordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<WordDetailDto>> UpdateAsync(
        uint wordId,
        UpdateWordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<BulkImportResultDto>> ImportCsvAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> SoftDeleteAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RestoreAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<Result<WordSenseDto>> CreateSenseAsync(
        uint wordId,
        CreateSenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<WordSenseDto>> UpdateSenseAsync(
        uint wordId,
        uint senseId,
        UpdateSenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> SoftDeleteSenseAsync(
        uint wordId,
        uint senseId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RestoreSenseAsync(
        uint wordId,
        uint senseId,
        CancellationToken cancellationToken = default);
}
