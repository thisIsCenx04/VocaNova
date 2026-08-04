using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> entity)
    {
        entity.HasKey(e => e.WordId).HasName("PRIMARY");

        entity.ToTable("words");

        entity.HasIndex(e => e.CefrLevel, "idx_words_cefr");

        entity.HasIndex(e => e.WordKey, "idx_words_key").IsUnique();

        entity.HasIndex(e => e.Status, "idx_words_status");

        entity.HasIndex(e => e.Word1, "idx_words_word");

        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CefrLevel)
            .HasColumnName("cefr_level")
            .HasMaxLength(2)
            .HasComment("A1/A2/B1/B2/C1/C2");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);
        entity.Property(e => e.IsPhrase)
            .HasColumnName("is_phrase")
            .HasComment("1=idiom/phrase/compound");
        entity.Property(e => e.PhoneticUk)
            .HasColumnName("phonetic_uk")
            .HasMaxLength(100);
        entity.Property(e => e.PhoneticUs)
            .HasColumnName("phonetic_us")
            .HasMaxLength(100);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'")
            .HasComment("active/deleted");
        entity.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.Word1)
            .HasMaxLength(150)
            .HasComment("Từ tiếng Anh — lowercase normalized")
            .HasColumnName("word");
        entity.Property(e => e.WordKey)
            .HasColumnName("word_key")
            .HasMaxLength(150)
            .HasComment("casefold+strip — lookup tránh duplicate")
            .UseCollation("utf8mb4_bin");
    }
}

