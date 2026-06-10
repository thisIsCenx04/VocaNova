using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserTopicPreferenceConfiguration : IEntityTypeConfiguration<UserTopicPreference>
{
    public void Configure(EntityTypeBuilder<UserTopicPreference> entity)
    {
        entity.HasKey(e => new { e.UserId, e.TopicId })
            .HasName("PRIMARY")
            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

        entity.ToTable("user_topic_preferences");

        entity.HasIndex(e => e.TopicId, "fk_tp_topic");

        entity.HasIndex(e => new { e.UserId, e.Status }, "idx_topic_pref_user_status");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.TopicId)
            .HasColumnName("topic_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(3)");
        entity.Property(e => e.Source)
            .HasColumnName("source")
            .HasMaxLength(20)
            .HasComment("knn_suggested/user_selected/onboarding");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");

        entity.HasOne(d => d.Topic).WithMany(p => p.UserTopicPreferences)
            .HasForeignKey(d => d.TopicId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_tp_topic");

        entity.HasOne(d => d.User).WithMany(p => p.UserTopicPreferences)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_tp_user");
    }
}

