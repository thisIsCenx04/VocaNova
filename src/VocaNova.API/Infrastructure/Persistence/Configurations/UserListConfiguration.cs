using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserListConfiguration : IEntityTypeConfiguration<UserList>
{
    public void Configure(EntityTypeBuilder<UserList> entity)
    {
        entity.HasKey(e => e.ListId).HasName("PRIMARY");

        entity.ToTable("user_lists");

        entity.HasIndex(e => e.UserId, "idx_user_lists_user");

        entity.HasIndex(e => new { e.UserId, e.Status }, "idx_user_lists_user_status");

        entity.Property(e => e.ListId)
            .HasColumnName("list_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("current_timestamp()")
            .HasColumnType("timestamp");
        entity.Property(e => e.ListName)
            .HasColumnName("list_name")
            .HasMaxLength(100);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'");
        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("int(10) unsigned");

        entity.HasOne(d => d.User).WithMany(p => p.UserLists)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_lists_user");
    }
}

