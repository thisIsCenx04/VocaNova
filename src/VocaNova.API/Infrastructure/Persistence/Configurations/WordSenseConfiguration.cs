using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordSenseConfiguration : IEntityTypeConfiguration<WordSense>
{
    public void Configure(EntityTypeBuilder<WordSense> entity)
    {
        entity.HasKey(e => e.SenseId).HasName("PRIMARY");

        entity.ToTable("word_senses");

        entity.HasIndex(e => e.WordId, "idx_senses_word");

        entity.HasIndex(e => new { e.WordId, e.WordClass }, "idx_senses_word_class");

        entity.Property(e => e.SenseId)
            .HasColumnName("sense_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.EnglishDefinition)
            .HasColumnName("english_definition")
            .HasColumnType("text");
        entity.Property(e => e.SenseOrder)
            .HasColumnName("sense_order")
            .HasComment("Thứ tự nghĩa: 1, 2, 3...")
            .HasColumnType("int(11)");
        entity.Property(e => e.VietnameseMeaning)
            .HasColumnName("vietnamese_meaning")
            .HasColumnType("text");
        entity.Property(e => e.WordClass)
            .HasColumnName("word_class")
            .HasMaxLength(30)
            .HasComment("noun/verb/adjective/adverb...");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Word).WithMany(p => p.WordSenses)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_senses_word");
    }
}

