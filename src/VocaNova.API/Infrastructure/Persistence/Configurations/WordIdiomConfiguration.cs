using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordIdiomConfiguration : IEntityTypeConfiguration<WordIdiom>
{
    public void Configure(EntityTypeBuilder<WordIdiom> entity)
    {
        entity.HasKey(e => e.IdiomId).HasName("PRIMARY");

        entity.ToTable("word_idioms");

        entity.HasIndex(e => e.WordId, "idx_idioms_word");

        entity.Property(e => e.IdiomId)
            .HasColumnName("idiom_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.IdiomText)
            .HasColumnName("idiom_text")
            .HasMaxLength(300);
        entity.Property(e => e.MeaningEn)
            .HasColumnName("meaning_en")
            .HasColumnType("text");
        entity.Property(e => e.MeaningVi)
            .HasColumnName("meaning_vi")
            .HasColumnType("text");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Word).WithMany(p => p.WordIdioms)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_idioms_word");
    }
}

