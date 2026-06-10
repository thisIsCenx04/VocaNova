using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class UserLearningProfile
{
    public uint UserId { get; set; }

    public uint? AgeRangeId { get; set; }

    public uint? RegionId { get; set; }

    public uint? OccupationId { get; set; }

    public uint? EducationLevelId { get; set; }

    public uint? LearningPurposeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AgeRange? AgeRange { get; set; }

    public virtual EducationLevel? EducationLevel { get; set; }

    public virtual LearningPurpose? LearningPurpose { get; set; }

    public virtual Occupation? Occupation { get; set; }

    public virtual Region? Region { get; set; }

    public virtual User User { get; set; } = null!;
}

