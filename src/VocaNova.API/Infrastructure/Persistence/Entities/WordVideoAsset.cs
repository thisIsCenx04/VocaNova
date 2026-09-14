namespace VocaNova.API.Infrastructure.Persistence.Entities;

// Database-first: synchronized from word_video_assets via scaffold-mysql.ps1.
public partial class WordVideoAsset
{
    public uint VideoId { get; set; }
    public uint WordId { get; set; }
    public string Source { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public string StorageUrl { get; set; } = null!;
    public string ThumbnailUrl { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public virtual Word Word { get; set; } = null!;
}
