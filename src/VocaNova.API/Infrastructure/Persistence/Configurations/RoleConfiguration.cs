using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.HasKey(e => e.RoleId).HasName("PRIMARY");

        entity.ToTable("roles");

        entity.HasIndex(e => e.RoleName, "uq_role_name").IsUnique();

        entity.Property(e => e.RoleId)
            .HasColumnName("role_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.RoleName)
            .HasColumnName("role_name")
            .HasMaxLength(30);
    }
}

