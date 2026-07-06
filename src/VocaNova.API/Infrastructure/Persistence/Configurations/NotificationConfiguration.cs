using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

        entity.ToTable("notifications");

        entity.HasIndex(e => new { e.UserId, e.IsRead }, "idx_notifications_user_read");

        entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_notifications_user_time");

        entity.Property(e => e.NotificationId)
            .HasColumnName("notification_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.Type)
            .HasColumnName("type")
            .HasMaxLength(50);
        entity.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(150);
        entity.Property(e => e.Message)
            .HasColumnName("message")
            .HasMaxLength(500);
        entity.Property(e => e.RefType)
            .HasColumnName("ref_type")
            .HasMaxLength(50);
        entity.Property(e => e.RefId)
            .HasColumnName("ref_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValueSql("0");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp");

        entity.HasOne<User>().WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_notifications_user");
    }
}
