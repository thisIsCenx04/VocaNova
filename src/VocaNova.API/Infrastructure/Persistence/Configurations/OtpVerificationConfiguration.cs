using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> entity)
    {
        entity.HasKey(e => e.OtpId).HasName("PRIMARY");

        entity.ToTable("otp_verifications");

        entity.HasIndex(e => e.ExpiresAt, "idx_otp_expires");

        entity.HasIndex(e => new { e.Phone, e.Status, e.ExpiresAt }, "idx_otp_phone_active");

        entity.HasIndex(e => new { e.Phone, e.CreatedAt }, "idx_otp_phone_time");

        entity.HasIndex(e => e.UserId, "idx_otp_user");

        entity.Property(e => e.OtpId)
            .HasColumnName("otp_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.OtpCode)
            .HasColumnName("otp_code")
            .HasMaxLength(6);
        entity.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");
        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.VerifyAttemptCount)
            .HasColumnName("verify_attempt_count")
            .HasColumnType("int(11)");

        entity.HasOne(d => d.User).WithMany(p => p.OtpVerifications)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("fk_otp_user");
    }
}

