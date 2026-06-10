using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class WordTopicConfiguration : IEntityTypeConfiguration<WordTopic>
{
    public void Configure(EntityTypeBuilder<WordTopic> entity)
    {
        entity.HasKey(e => new { e.WordId, e.TopicId })
            .HasName("PRIMARY")
            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

        entity.ToTable("word_topics");

        entity.HasIndex(e => e.TopicId, "idx_word_topics_topic");

        entity.Property(e => e.WordId)
            .HasColumnName("word_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.TopicId)
            .HasColumnName("topic_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.Topic).WithMany(p => p.WordTopics)
            .HasForeignKey(d => d.TopicId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_wt_topic");

        entity.HasOne(d => d.Word).WithMany(p => p.WordTopics)
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_wt_word");
    }
}

