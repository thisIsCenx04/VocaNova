using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordRelationConfiguration : IEntityTypeConfiguration<WordRelation>
{
    public void Configure(EntityTypeBuilder<WordRelation> entity)
    {
        entity.HasKey(e => e.RelationId).HasName("PRIMARY");

        entity.ToTable("word_relations");

        entity.HasIndex(e => new { e.WordId, e.RelationType, e.IsQuizEligible }, "idx_relations_quiz");

        entity.HasIndex(e => e.RelatedWordId, "idx_relations_related_word_id");

        entity.HasIndex(e => e.SenseId, "idx_relations_sense");

        entity.HasIndex(e => e.RelatedWord, "idx_relations_target_text");

        entity.HasIndex(e => new { e.WordId, e.RelationType }, "idx_relations_word_type");

        entity.Property(e => e.RelationId)
            .HasColumnName("relation_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.IsQuizEligible)
            .HasColumnName("is_quiz_eligible")
            .IsRequired()
            .HasDefaultValueSql("'1'");
        entity.Property(e => e.RelatedWord)
            .HasColumnName("related_word")
            .HasMaxLength(150);
        entity.Property(e => e.RelatedWordId)
            .HasColumnName("related_word_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.RelationType)
            .HasColumnName("relation_type")
            .HasMaxLength(10)
            .HasComment("synonym/antonym");
        entity.Property(e => e.SenseId)
            .HasColumnName("sense_id")
            .HasComment("null = áp dụng cho toàn bộ từ")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.RelatedWordNavigation).WithMany(p => p.WordRelationrelatedWordNavigations)
            .HasForeignKey(d => d.RelatedWordId)
            .HasConstraintName("fk_relations_related_word");

        entity.HasOne(d => d.Sense).WithMany(p => p.WordRelations)
            .HasForeignKey(d => d.SenseId)
            .HasConstraintName("fk_relations_sense");

        entity.HasOne(d => d.Word).WithMany(p => p.WordRelationwords)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_relations_word");
    }
}

