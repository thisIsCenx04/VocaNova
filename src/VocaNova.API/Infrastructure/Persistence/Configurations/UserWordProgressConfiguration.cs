using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserWordProgressConfiguration : IEntityTypeConfiguration<UserWordProgress>
{
    public void Configure(EntityTypeBuilder<UserWordProgress> entity)
    {
        entity.HasKey(e => e.ProgressId).HasName("PRIMARY");

        entity.ToTable("user_word_progress");

        entity.HasIndex(e => e.WordId, "fk_prog_word");

        entity.HasIndex(e => new { e.UserId, e.NextReviewAt }, "idx_progress_review");

        entity.HasIndex(e => new { e.UserId, e.WordId }, "idx_progress_user_word").IsUnique();

        entity.HasIndex(e => new { e.UserId, e.IsInWrongList }, "idx_progress_wrong_list");

        entity.Property(e => e.ProgressId)
            .HasColumnName("progress_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.ConsecutiveCorrect)
            .HasColumnName("consecutive_correct")
            .HasColumnType("int(11)");
        entity.Property(e => e.CorrectCount)
            .HasColumnName("correct_count")
            .HasColumnType("int(11)");
        entity.Property(e => e.EaseFactor)
            .HasColumnName("ease_factor")
            .HasDefaultValueSql("'2.5'");
        entity.Property(e => e.IsInWrongList)
            .HasColumnName("is_in_wrong_list");
        entity.Property(e => e.LastTestedAt)
            .HasColumnName("last_tested_at")
            .HasColumnType("timestamp");
        entity.Property(e => e.LastWrongAt)
            .HasColumnName("last_wrong_at")
            .HasColumnType("timestamp");
        entity.Property(e => e.MasteryLevel)
            .HasColumnName("mastery_level")
            .HasColumnType("int(11)");
        entity.Property(e => e.NextReviewAt)
            .HasColumnName("next_review_at")
            .HasColumnType("timestamp");
        entity.Property(e => e.SrsInterval)
            .HasColumnName("srs_interval")
            .HasDefaultValueSql("'1'")
            .HasColumnType("int(11)");
        entity.Property(e => e.TestCount)
            .HasColumnName("test_count")
            .HasColumnType("int(11)");
        entity.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WrongCount)
            .HasColumnName("wrong_count")
            .HasColumnType("int(11)");

        entity.HasOne(d => d.User).WithMany(p => p.UserWordProgresses)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_prog_user");

        entity.HasOne(d => d.Word).WithMany(p => p.UserWordProgresses)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_prog_word");
    }
}

