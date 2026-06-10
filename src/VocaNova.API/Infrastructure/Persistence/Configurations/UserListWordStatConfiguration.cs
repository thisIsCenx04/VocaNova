using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserListWordStatConfiguration : IEntityTypeConfiguration<UserListWordStat>
{
    public void Configure(EntityTypeBuilder<UserListWordStat> entity)
    {
        entity.HasKey(e => new { e.UserId, e.ListId, e.WordId })
            .HasName("PRIMARY")
            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

        entity.ToTable("user_list_word_stats");

        entity.HasIndex(e => e.WordId, "fk_lws_word");

        entity.HasIndex(e => new { e.ListId, e.WordId }, "idx_list_stats_list_word");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.ListId)
            .HasColumnName("list_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CorrectCount)
            .HasColumnName("correct_count")
            .HasColumnType("int(11)");
        entity.Property(e => e.LastTestedAt)
            .HasColumnName("last_tested_at")
            .HasColumnType("timestamp");
        entity.Property(e => e.WrongCount)
            .HasColumnName("wrong_count")
            .HasColumnType("int(11)");

        entity.HasOne(d => d.List).WithMany(p => p.UserListWordStats)
            .HasForeignKey(d => d.ListId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_lws_list");

        entity.HasOne(d => d.User).WithMany(p => p.UserListWordStats)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_lws_user");

        entity.HasOne(d => d.Word).WithMany(p => p.UserListWordStats)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_lws_word");
    }
}

