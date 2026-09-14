using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordVideoAssetConfiguration : IEntityTypeConfiguration<WordVideoAsset>
{
    public void Configure(EntityTypeBuilder<WordVideoAsset> entity)
    {
        entity.ToTable("word_video_assets");
        entity.HasKey(e => e.VideoId).HasName("PRIMARY");
        entity.HasIndex(e => e.WordId, "idx_video_word").IsUnique();
        entity.Property(e => e.VideoId).HasColumnName("video_id").HasColumnType("int unsigned");
        entity.Property(e => e.WordId).HasColumnName("word_id").HasColumnType("int unsigned");
        entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(20).HasDefaultValueSql("'uploaded'");
        entity.Property(e => e.PublicId).HasColumnName("public_id").HasMaxLength(255).UseCollation("utf8mb4_bin");
        entity.Property(e => e.StorageUrl).HasColumnName("storage_url").HasMaxLength(500);
        entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(500);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValueSql("'active'").HasComment("active/deleted");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.HasOne(e => e.Word).WithOne(e => e.WordVideoAsset)
            .HasForeignKey<WordVideoAsset>(e => e.WordId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_video_word");
    }
}
