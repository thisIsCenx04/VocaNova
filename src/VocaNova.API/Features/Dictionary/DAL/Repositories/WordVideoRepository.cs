using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Dictionary.DAL.Repositories;

public sealed class WordVideoRepository(VocaNovaDbContext db) : IWordVideoRepository
{
    public Task<bool> WordExistsAsync(uint wordId, CancellationToken cancellationToken = default) =>
        db.Words.AnyAsync(w => w.WordId == wordId, cancellationToken);

    public Task<bool> IsReferencedAsync(string publicId, CancellationToken cancellationToken = default) =>
        db.WordVideoAssets.IgnoreQueryFilters().AsNoTracking().AnyAsync(v => v.PublicId == publicId, cancellationToken);

    public async Task<VideoReplacement?> ReplaceAsync(uint wordId, StoredWordVideo video, CancellationToken cancellationToken = default)
    {
        await using var transaction = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var word = await LockWordAsync(wordId, cancellationToken);
        if (word is null) return null;
        var asset = await db.WordVideoAssets.IgnoreQueryFilters().SingleOrDefaultAsync(v => v.WordId == wordId, cancellationToken);
        var previousPublicId = asset?.PublicId;
        if (asset is null)
        {
            asset = new WordVideoAsset { WordId = wordId, CreatedAt = DateTime.UtcNow };
            db.WordVideoAssets.Add(asset);
        }
        asset.Source = "uploaded";
        asset.PublicId = video.PublicId;
        asset.StorageUrl = video.Url;
        asset.ThumbnailUrl = video.ThumbnailUrl;
        asset.Status = "active";
        word.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new VideoReplacement(new WordVideo(asset.VideoId, asset.Source, asset.StorageUrl, asset.ThumbnailUrl, asset.Status), previousPublicId);
    }

    public async Task<bool> SoftDeleteAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        await using var transaction = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var word = await LockWordAsync(wordId, cancellationToken);
        if (word is null) return false;
        var asset = await db.WordVideoAssets.SingleOrDefaultAsync(v => v.WordId == wordId, cancellationToken);
        if (asset is null) return false;
        asset.Status = "deleted";
        word.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<Word?> LockWordAsync(uint wordId, CancellationToken cancellationToken)
    {
        // Serialize creation/replacement/deletion per word, including an absent video row.
        // Provider I/O has already finished before this short relational transaction.
        if (db.Database.IsRelational())
        {
            var words = await db.Words.FromSqlInterpolated($"SELECT * FROM words WHERE word_id = {wordId} AND status = 'active' FOR UPDATE")
                .IgnoreQueryFilters().ToListAsync(cancellationToken);
            return words.SingleOrDefault();
        }
        return await db.Words.SingleOrDefaultAsync(w => w.WordId == wordId, cancellationToken);
    }
}
