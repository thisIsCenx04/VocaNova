using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Persistence.Configurations;

public sealed class UserLearningProfileConfiguration : IEntityTypeConfiguration<UserLearningProfile>
{
    public void Configure(EntityTypeBuilder<UserLearningProfile> entity)
    {
        entity.HasKey(e => e.UserId).HasName("PRIMARY");

        entity.ToTable("user_learning_profiles");

        entity.HasIndex(e => e.AgeRangeId, "idx_ulp_age_range");

        entity.HasIndex(e => e.EducationLevelId, "idx_ulp_education");

        entity.HasIndex(e => e.OccupationId, "idx_ulp_occupation");

        entity.HasIndex(e => e.LearningPurposeId, "idx_ulp_purpose");

        entity.HasIndex(e => e.RegionId, "idx_ulp_region");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever()
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.AgeRangeId)
            .HasColumnName("age_range_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(3)");
        entity.Property(e => e.EducationLevelId)
            .HasColumnName("education_level_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.LearningPurposeId)
            .HasColumnName("learning_purpose_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.OccupationId)
            .HasColumnName("occupation_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.RegionId)
            .HasColumnName("region_id")
            .HasColumnType("int(10) unsigned");
        entity.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(3)");

        entity.HasOne(d => d.AgeRange).WithMany(p => p.UserLearningProfiles)
            .HasForeignKey(d => d.AgeRangeId)
            .HasConstraintName("fk_ulp_age");

        entity.HasOne(d => d.EducationLevel).WithMany(p => p.UserLearningProfiles)
            .HasForeignKey(d => d.EducationLevelId)
            .HasConstraintName("fk_ulp_edu");

        entity.HasOne(d => d.LearningPurpose).WithMany(p => p.UserLearningProfiles)
            .HasForeignKey(d => d.LearningPurposeId)
            .HasConstraintName("fk_ulp_purpose");

        entity.HasOne(d => d.Occupation).WithMany(p => p.UserLearningProfiles)
            .HasForeignKey(d => d.OccupationId)
            .HasConstraintName("fk_ulp_occ");

        entity.HasOne(d => d.Region).WithMany(p => p.UserLearningProfiles)
            .HasForeignKey(d => d.RegionId)
            .HasConstraintName("fk_ulp_region");

        entity.HasOne(d => d.User).WithOne(p => p.UserLearningProfile)
            .HasForeignKey<UserLearningProfile>(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_ulp_user");
    }
}

