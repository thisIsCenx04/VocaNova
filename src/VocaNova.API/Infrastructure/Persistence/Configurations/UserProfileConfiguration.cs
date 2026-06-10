using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> entity)
    {
        entity.HasKey(e => e.UserId).HasName("PRIMARY");

        entity.ToTable("user_profiles");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever()
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);
        entity.Property(e => e.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(150);
        entity.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(3)");

        entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
            .HasForeignKey<UserProfile>(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_profiles_user");
    }
}

