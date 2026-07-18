using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class AiGradingCacheConfiguration : IEntityTypeConfiguration<AiGradingCache>
{
    public void Configure(EntityTypeBuilder<AiGradingCache> entity)
    {
        entity.HasKey(e => e.CacheId).HasName("PRIMARY");

        entity.ToTable("ai_grading_cache");

        entity.HasIndex(e => e.ExpiresAt, "idx_cache_expires");

        entity.HasIndex(e => e.WordId, "idx_cache_word");

        entity.HasIndex(e => e.CacheKey, "uq_cache_key").IsUnique();

        entity.Property(e => e.CacheId)
            .HasColumnName("cache_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.AiExplanation)
            .HasColumnName("ai_explanation")
            .HasColumnType("text");
        entity.Property(e => e.AiScore)
            .HasColumnName("ai_score");
        entity.Property(e => e.AiSuggestion)
            .HasColumnName("ai_suggestion")
            .HasColumnType("text");
        entity.Property(e => e.CacheKey)
            .HasColumnName("cache_key")
            .HasMaxLength(256);
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.ExpectedAnswer)
            .HasColumnName("expected_answer")
            .HasColumnType("text");
        entity.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .HasDefaultValueSql("'0000-00-00 00:00:00'")
            .HasColumnType("timestamp");
        entity.Property(e => e.HitCount)
            .HasColumnName("hit_count")
            .HasDefaultValueSql("'1'")
            .HasColumnType("int(11)");
        entity.Property(e => e.QuestionType)
            .HasColumnName("question_type")
            .HasColumnType("int(11)");
        entity.Property(e => e.UserAnswerNormalized)
            .HasColumnName("user_answer_normalized")
            .HasColumnType("text");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Word).WithMany(p => p.AiGradingCaches)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_cache_word");
    }
}

