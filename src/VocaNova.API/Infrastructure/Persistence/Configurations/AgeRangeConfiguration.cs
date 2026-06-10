using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class AgeRangeConfiguration : IEntityTypeConfiguration<AgeRange>
{
    public void Configure(EntityTypeBuilder<AgeRange> entity)
    {
        entity.HasKey(e => e.AgeRangeId).HasName("PRIMARY");

        entity.ToTable("age_ranges");

        entity.Property(e => e.AgeRangeId)
            .HasColumnName("age_range_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .HasColumnType("int(11)");
        entity.Property(e => e.MaxAge)
            .HasColumnName("max_age")
            .HasColumnType("int(11)");
        entity.Property(e => e.MinAge)
            .HasColumnName("min_age")
            .HasColumnType("int(11)");
        entity.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(50);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");
    }
}

