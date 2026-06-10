using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserAuthConfiguration : IEntityTypeConfiguration<UserAuth>
{
    public void Configure(EntityTypeBuilder<UserAuth> entity)
    {
        entity.HasKey(e => e.UserId).HasName("PRIMARY");

        entity.ToTable("user_auth");

        entity.HasIndex(e => e.GoogleUid, "idx_auth_google").IsUnique();

        entity.HasIndex(e => e.Phone, "idx_auth_phone").IsUnique();

        entity.HasIndex(e => e.Username, "idx_auth_username").IsUnique();

        entity.HasIndex(e => e.GoogleEmail, "uq_auth_google_email").IsUnique();

        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever()
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.GoogleEmail)
            .HasColumnName("google_email")
            .HasMaxLength(254);
        entity.Property(e => e.GoogleUid)
            .HasColumnName("google_uid")
            .HasMaxLength(200);
        entity.Property(e => e.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255);
        entity.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);
        entity.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(3)");
        entity.Property(e => e.Username)
            .HasColumnName("username")
            .HasMaxLength(100);

        entity.HasOne(d => d.User).WithOne(p => p.UserAuth)
            .HasForeignKey<UserAuth>(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_auth_user");
    }
}

