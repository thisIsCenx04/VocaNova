using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class TestSessionTopicConfiguration : IEntityTypeConfiguration<TestSessionTopic>
{
    public void Configure(EntityTypeBuilder<TestSessionTopic> entity)
    {
        entity.HasKey(e => new { e.SessionId, e.TopicId })
            .HasName("PRIMARY")
            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

        entity.ToTable("test_session_topics");

        entity.HasIndex(e => e.TopicId, "idx_session_topics_topic");

        entity.Property(e => e.SessionId)
            .HasColumnName("session_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.TopicId)
            .HasColumnName("topic_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Session).WithMany(p => p.TestSessionTopics)
            .HasForeignKey(d => d.SessionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_tst_session");

        entity.HasOne(d => d.Topic).WithMany(p => p.TestSessionTopics)
            .HasForeignKey(d => d.TopicId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_tst_topic");
    }
}

