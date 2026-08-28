using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Services.IServices;

public interface IWordAdminService
{
    Task<DictionaryResult<PagedCollection<AdminWordListItem>>> SearchAsync(AdminWordQuery query, CancellationToken cancellationToken = default);
    Task<DictionaryResult<WordDetail>> CreateAsync(CreateWordCommand command, CancellationToken cancellationToken = default);
    Task<DictionaryResult<WordDetail>> UpdateAsync(uint wordId, UpdateWordCommand command, CancellationToken cancellationToken = default);
    Task<DictionaryResult<BulkImportResult>> ImportCsvAsync(UploadedContent? content, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> SoftDeleteAsync(uint wordId, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> RestoreAsync(uint wordId, CancellationToken cancellationToken = default);
    Task<DictionaryResult<WordDetail>> UploadImageAsync(uint wordId, UploadedContent? content, CancellationToken cancellationToken = default);
    Task<DictionaryResult<WordDetail>> UpdateImageUrlAsync(uint wordId, string? imageUrl, CancellationToken cancellationToken = default);
    Task<DictionaryResult<WordAudio>> UploadAudioAsync(uint wordId, string? accent, UploadedContent? content, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> SoftDeleteAudioAsync(uint wordId, uint audioId, CancellationToken cancellationToken = default);
    Task<DictionaryResult<WordSense>> CreateSenseAsync(uint wordId, CreateSenseCommand command, CancellationToken cancellationToken = default);
    Task<DictionaryResult<WordSense>> UpdateSenseAsync(uint wordId, uint senseId, UpdateSenseCommand command, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> SoftDeleteSenseAsync(uint wordId, uint senseId, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> RestoreSenseAsync(uint wordId, uint senseId, CancellationToken cancellationToken = default);
}
