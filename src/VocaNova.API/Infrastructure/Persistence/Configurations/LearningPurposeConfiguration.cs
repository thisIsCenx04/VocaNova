using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class LearningPurposeConfiguration : IEntityTypeConfiguration<LearningPurpose>
{
    public void Configure(EntityTypeBuilder<LearningPurpose> entity)
    {
        entity.HasKey(e => e.LearningPurposeId).HasName("PRIMARY");

        entity.ToTable("learning_purposes");

        entity.Property(e => e.LearningPurposeId)
            .HasColumnName("learning_purpose_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(255);
        entity.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");
    }
}

