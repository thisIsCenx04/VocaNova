using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasKey(e => e.TokenId).HasName("PRIMARY");

        entity.ToTable("refresh_tokens");

        entity.HasIndex(e => e.ExpiresAt, "idx_rt_expires");

        entity.HasIndex(e => e.TokenHash, "idx_rt_hash").IsUnique();

        entity.HasIndex(e => e.UserId, "idx_rt_user");

        entity.HasIndex(e => new { e.UserId, e.RevokedAt }, "idx_rt_user_active");

        entity.Property(e => e.TokenId)
            .HasColumnName("token_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.DeviceInfo)
            .HasColumnName("device_info")
            .HasMaxLength(255);
        entity.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);
        entity.Property(e => e.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp");
        entity.Property(e => e.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255);
        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_rt_user");
    }
}

