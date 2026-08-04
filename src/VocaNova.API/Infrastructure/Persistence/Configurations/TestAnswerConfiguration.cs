using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class TestAnswerConfiguration : IEntityTypeConfiguration<TestAnswer>
{
    public void Configure(EntityTypeBuilder<TestAnswer> entity)
    {
        entity.HasKey(e => e.AnswerId).HasName("PRIMARY");

        entity.ToTable("test_answers");

        entity.HasIndex(e => e.SenseId, "idx_answers_sense");

        entity.HasIndex(e => e.SessionId, "idx_answers_session");

        entity.HasIndex(e => e.WordId, "idx_answers_word");

        entity.Property(e => e.AnswerId)
            .HasColumnName("answer_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.AcceptedAnswers)
            .HasColumnName("accepted_answers")
            .HasColumnType("json");
        entity.Property(e => e.AiExplanation)
            .HasColumnName("ai_explanation")
            .HasColumnType("text");
        entity.Property(e => e.AiScore)
            .HasColumnName("ai_score");
        entity.Property(e => e.AiSuggestion)
            .HasColumnName("ai_suggestion")
            .HasColumnType("text");
        entity.Property(e => e.DisplayContent)
            .HasColumnName("display_content")
            .HasColumnType("text");
        entity.Property(e => e.ExpectedAnswer)
            .HasColumnName("expected_answer")
            .HasColumnType("text");
        entity.Property(e => e.IsCorrect)
            .HasColumnName("is_correct");
        entity.Property(e => e.QuestionNumber)
            .HasColumnName("question_number")
            .HasColumnType("int(11)");
        entity.Property(e => e.QuestionType)
            .HasColumnName("question_type")
            .HasColumnType("int(11)");
        entity.Property(e => e.SenseId)
            .HasColumnName("sense_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.SessionId)
            .HasColumnName("session_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.UserAnswer)
            .HasColumnName("user_answer")
            .HasColumnType("text");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Sense).WithMany(p => p.TestAnswers)
            .HasForeignKey(d => d.SenseId)
            .HasConstraintName("fk_ans_sense");

        entity.HasOne(d => d.Session).WithMany(p => p.TestAnswers)
            .HasForeignKey(d => d.SessionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_ans_session");

        entity.HasOne(d => d.Word).WithMany(p => p.TestAnswers)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_ans_word");
    }
}

