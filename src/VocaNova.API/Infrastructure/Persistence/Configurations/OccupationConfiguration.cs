using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class OccupationConfiguration : IEntityTypeConfiguration<Occupation>
{
    public void Configure(EntityTypeBuilder<Occupation> entity)
    {
        entity.HasKey(e => e.OccupationId).HasName("PRIMARY");

        entity.ToTable("occupations");

        entity.Property(e => e.OccupationId)
            .HasColumnName("occupation_id")
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

