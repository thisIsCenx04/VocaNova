using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Services.IServices;

public interface IWordVideoService
{
    Task<DictionaryResult<WordVideo>> SaveAsync(uint wordId, UploadedContent? content, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> DeleteAsync(uint wordId, CancellationToken cancellationToken = default);
}
