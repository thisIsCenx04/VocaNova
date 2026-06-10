using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class LearningPurpose
{
    public uint LearningPurposeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<UserLearningProfile> UserLearningProfiles { get; set; } = new List<UserLearningProfile>();
}

