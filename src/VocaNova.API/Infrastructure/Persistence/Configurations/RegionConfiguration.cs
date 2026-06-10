using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> entity)
    {
        entity.HasKey(e => e.RegionId).HasName("PRIMARY");

        entity.ToTable("regions");

        entity.HasIndex(e => e.ParentId, "idx_region_parent");

        entity.HasIndex(e => e.Code, "uq_region_code").IsUnique();

        entity.Property(e => e.RegionId)
            .HasColumnName("region_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Code)
            .HasColumnName("code")
            .HasMaxLength(10);
        entity.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100);
        entity.Property(e => e.ParentId)
            .HasColumnName("parent_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");

        entity.HasOne(d => d.Parent).WithMany(p => p.Inverseparent)
            .HasForeignKey(d => d.ParentId)
            .HasConstraintName("fk_region_parent");
    }
}

