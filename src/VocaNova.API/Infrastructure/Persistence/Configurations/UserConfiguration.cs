using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.UserId).HasName("PRIMARY");

        entity.ToTable("users");

        entity.HasIndex(e => e.RoleId, "idx_users_role");

        entity.HasIndex(e => e.Status, "idx_users_status");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(3)");
        entity.Property(e => e.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("datetime(3)");
        entity.Property(e => e.RoleId)
            .HasColumnName("role_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");
        entity.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(3)");

        entity.HasOne(d => d.Role).WithMany(p => p.Users)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_users_role");
    }
}

