using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class EducationLevelConfiguration : IEntityTypeConfiguration<EducationLevel>
{
    public void Configure(EntityTypeBuilder<EducationLevel> entity)
    {
        entity.HasKey(e => e.EducationLevelId).HasName("PRIMARY");

        entity.ToTable("education_levels");

        entity.Property(e => e.EducationLevelId)
            .HasColumnName("education_level_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(255);
        entity.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .HasColumnType("int(11)");
        entity.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");
    }
}

