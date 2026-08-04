using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> entity)
    {
        entity.HasKey(e => e.TopicId).HasName("PRIMARY");

        entity.ToTable("topics");

        entity.HasIndex(e => e.TopicName, "uq_topic_name").IsUnique();

        entity.Property(e => e.TopicId)
            .HasColumnName("topic_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Icon)
            .HasColumnName("icon")
            .HasMaxLength(20);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");
        entity.Property(e => e.TopicName)
            .HasColumnName("topic_name")
            .HasMaxLength(50);
        entity.Property(e => e.TopicNameVi)
            .HasColumnName("topic_name_vi")
            .HasMaxLength(50);
    }
}

