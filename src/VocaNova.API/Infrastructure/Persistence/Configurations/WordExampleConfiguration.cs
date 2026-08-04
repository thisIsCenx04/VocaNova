using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordExampleConfiguration : IEntityTypeConfiguration<WordExample>
{
    public void Configure(EntityTypeBuilder<WordExample> entity)
    {
        entity.HasKey(e => e.ExampleId).HasName("PRIMARY");

        entity.ToTable("word_examples");

        entity.HasIndex(e => e.SenseId, "idx_examples_sense");

        entity.HasIndex(e => e.WordId, "idx_examples_word");

        entity.Property(e => e.ExampleId)
            .HasColumnName("example_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.ExampleEn)
            .HasColumnName("example_en")
            .HasColumnType("text");
        entity.Property(e => e.ExampleVi)
            .HasColumnName("example_vi")
            .HasColumnType("text");
        entity.Property(e => e.OrderIndex)
            .HasColumnName("order_index")
            .HasColumnType("int(11)");
        entity.Property(e => e.SenseId)
            .HasColumnName("sense_id")
            .HasComment("null = ví dụ chung cho cả từ")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Sense).WithMany(p => p.WordExamples)
            .HasForeignKey(d => d.SenseId)
            .HasConstraintName("fk_examples_sense");

        entity.HasOne(d => d.Word).WithMany(p => p.WordExamples)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_examples_word");
    }
}

