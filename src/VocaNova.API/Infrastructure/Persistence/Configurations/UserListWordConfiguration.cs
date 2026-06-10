using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserListWordConfiguration : IEntityTypeConfiguration<UserListWord>
{
    public void Configure(EntityTypeBuilder<UserListWord> entity)
    {
        entity.HasKey(e => new { e.UserId, e.ListId, e.WordId })
            .HasName("PRIMARY")
            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

        entity.ToTable("user_list_words");

        entity.HasIndex(e => e.WordId, "fk_lw_word");

        entity.HasIndex(e => e.ListId, "idx_list_words_list");

        entity.HasIndex(e => new { e.UserId, e.WordId }, "idx_list_words_user_word");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.ListId)
            .HasColumnName("list_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.AddMethod)
            .HasColumnName("add_method")
            .HasMaxLength(20);
        entity.Property(e => e.AddedAt)
            .HasColumnName("added_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(1000);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");

        entity.HasOne(d => d.List).WithMany(p => p.UserListWords)
            .HasForeignKey(d => d.ListId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_lw_list");

        entity.HasOne(d => d.User).WithMany(p => p.UserListWords)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_lw_user");

        entity.HasOne(d => d.Word).WithMany(p => p.UserListWords)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_lw_word");
    }
}

