using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordDerivedFormConfiguration : IEntityTypeConfiguration<WordDerivedForm>
{
    public void Configure(EntityTypeBuilder<WordDerivedForm> entity)
    {
        entity.HasKey(e => e.DerivedId).HasName("PRIMARY");

        entity.ToTable("word_derived_forms");

        entity.HasIndex(e => e.DerivedWord, "idx_derived_text");

        entity.HasIndex(e => e.WordId, "idx_derived_word");

        entity.HasIndex(e => e.DerivedWordId, "idx_derived_word_id");

        entity.Property(e => e.DerivedId)
            .HasColumnName("derived_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.DerivedWord)
            .HasColumnName("derived_word")
            .HasMaxLength(150);
        entity.Property(e => e.DerivedWordId)
            .HasColumnName("derived_word_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WordClass)
            .HasColumnName("word_class")
            .HasMaxLength(30);
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.DerivedWordNavigation).WithMany(p => p.WordDerivedFormderivedWordNavigations)
            .HasForeignKey(d => d.DerivedWordId)
            .HasConstraintName("fk_derived_word_ref");

        entity.HasOne(d => d.Word).WithMany(p => p.WordDerivedFormwords)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_derived_word");
    }
}

