using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.HasKey(e => e.LogId).HasName("PRIMARY");

        entity.ToTable("audit_logs");

        entity.HasIndex(e => new { e.EntityType, e.EntityId }, "idx_audit_entity");

        entity.HasIndex(e => e.CreatedAt, "idx_audit_time");

        entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_audit_user_time");

        entity.Property(e => e.LogId)
            .HasColumnName("log_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(50);
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.EntityId)
            .HasColumnName("entity_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50);
        entity.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);
        entity.Property(e => e.PayloadAfter)
            .HasColumnName("payload_after")
            .HasColumnType("json");
        entity.Property(e => e.PayloadBefore)
            .HasColumnName("payload_before")
            .HasColumnType("json");
        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_audit_user");
    }
}

