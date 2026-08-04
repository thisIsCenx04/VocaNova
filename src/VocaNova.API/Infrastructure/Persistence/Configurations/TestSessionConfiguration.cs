using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class TestSessionConfiguration : IEntityTypeConfiguration<TestSession>
{
    public void Configure(EntityTypeBuilder<TestSession> entity)
    {
        entity.HasKey(e => e.SessionId).HasName("PRIMARY");

        entity.ToTable("test_sessions");

        entity.HasIndex(e => new { e.UserId, e.Status }, "idx_test_sessions_user_status");

        entity.HasIndex(e => new { e.UserId, e.StartedAt }, "idx_test_sessions_user_time");

        entity.Property(e => e.SessionId)
            .HasColumnName("session_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CorrectCount)
            .HasColumnName("correct_count")
            .HasColumnType("int(11)");
        entity.Property(e => e.EndedAt)
            .HasColumnName("ended_at")
            .HasColumnType("timestamp");
        entity.Property(e => e.Lives)
            .HasColumnName("lives")
            .HasColumnType("int(11)");
        entity.Property(e => e.MaxStreak)
            .HasColumnName("max_streak")
            .HasColumnType("int(11)");
        entity.Property(e => e.Mode)
            .HasColumnName("mode")
            .HasMaxLength(20);
        entity.Property(e => e.QuestionCount)
            .HasColumnName("question_count")
            .HasColumnType("int(11)");
        entity.Property(e => e.QuestionType)
            .HasColumnName("question_type")
            .HasColumnType("int(11)");
        entity.Property(e => e.Score)
            .HasColumnName("score");
        entity.Property(e => e.ScopeDateFrom)
            .HasColumnName("scope_date_from")
            .HasColumnType("date");
        entity.Property(e => e.ScopeDateTo)
            .HasColumnName("scope_date_to")
            .HasColumnType("date");
        entity.Property(e => e.ScopeType)
            .HasColumnName("scope_type")
            .HasMaxLength(20);
        entity.Property(e => e.StartedAt)
            .HasColumnName("started_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20);
        entity.Property(e => e.TestType)
            .HasColumnName("test_type")
            .HasMaxLength(20);
        entity.Property(e => e.TimeLimitSec)
            .HasColumnName("time_limit_sec")
            .HasColumnType("int(11)");
        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WordLimit)
            .HasColumnName("word_limit")
            .HasColumnType("int(11)");
        entity.Property(e => e.WordOrder)
            .HasColumnName("word_order")
            .HasMaxLength(20);
        entity.Property(e => e.WrongCount)
            .HasColumnName("wrong_count")
            .HasColumnType("int(11)");

        entity.HasOne(d => d.User).WithMany(p => p.TestSessions)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_ts_user");
    }
}

