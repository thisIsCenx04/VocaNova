using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordAudioAssetConfiguration : IEntityTypeConfiguration<WordAudioAsset>
{
    public void Configure(EntityTypeBuilder<WordAudioAsset> entity)
    {
        entity.HasKey(e => e.AudioId).HasName("PRIMARY");

        entity.ToTable("word_audio_assets");

        entity.HasIndex(e => e.WordId, "idx_audio_word");

        entity.HasIndex(e => new { e.WordId, e.Accent }, "idx_audio_word_accent").IsUnique();

        entity.Property(e => e.AudioId)
            .HasColumnName("audio_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Accent)
            .HasColumnName("accent")
            .HasMaxLength(10)
            .HasDefaultValueSql("'uk'")
            .HasComment("uk/us");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.Source)
            .HasColumnName("source")
            .HasMaxLength(20)
            .HasDefaultValueSql("'original'")
            .HasComment("original/tts/uploaded");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasComment("pending/uploaded/missing/tts_generated");
        entity.Property(e => e.StorageUrl)
            .HasColumnName("storage_url")
            .HasMaxLength(500);
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Word).WithMany(p => p.WordAudioAssets)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_audio_word");
    }
}

