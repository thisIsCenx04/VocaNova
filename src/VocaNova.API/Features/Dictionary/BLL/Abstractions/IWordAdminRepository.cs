using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordAdminRepository
{
    Task<PagedCollection<AdminWordListItem>> SearchAsync(AdminWordQuery query, CancellationToken cancellationToken = default);
    Task<bool> WordKeyExistsAsync(string wordKey, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> WordExistsAsync(uint wordId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<bool> SenseExistsAsync(uint wordId, uint senseId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<bool> MatchingSenseExistsAsync(uint wordId, string wordClass, string englishDefinition, CancellationToken cancellationToken = default);
    Task<uint?> FindWordIdByKeyAsync(string wordKey, CancellationToken cancellationToken = default);
    Task<WordDetail> CreateAsync(CreateWordCommand command, CancellationToken cancellationToken = default);
    Task<WordDetail> CreateWithSenseAsync(CreateWordCommand word, CreateSenseCommand sense, CancellationToken cancellationToken = default);
    Task<WordDetail?> UpdateMetadataAsync(uint wordId, UpdateWordCommand command, CancellationToken cancellationToken = default);
    Task<bool?> UpdateMissingImportMetadataAsync(uint wordId, ImportWordMetadata metadata, CancellationToken cancellationToken = default);
    Task<bool> SetWordStatusAsync(uint wordId, string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<uint>> GetReferencingUserIdsAsync(uint wordId, CancellationToken cancellationToken = default);
    Task<WordDetail?> SetImageUrlAsync(uint wordId, string? url, CancellationToken cancellationToken = default);
    Task<WordAudio?> UpsertAudioAsync(uint wordId, StoredMedia media, string? accent, CancellationToken cancellationToken = default);
    Task<bool> SetAudioStatusAsync(uint wordId, uint audioId, string status, CancellationToken cancellationToken = default);
    Task<WordSense?> CreateSenseAsync(uint wordId, CreateSenseCommand command, CancellationToken cancellationToken = default);
    Task<WordSense?> UpdateSenseAsync(uint wordId, uint senseId, UpdateSenseCommand command, CancellationToken cancellationToken = default);
    Task<bool> SetSenseStatusAsync(uint wordId, uint senseId, string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, uint>> FindActiveTopicIdsByNamesAsync(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default);
    Task<int> AddTopicsAsync(uint wordId, IReadOnlyCollection<uint> topicIds, CancellationToken cancellationToken = default);
}
