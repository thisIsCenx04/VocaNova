using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class AgeRange
{
    public uint AgeRangeId { get; set; }

    public string Name { get; set; } = null!;

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public int DisplayOrder { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<UserLearningProfile> UserLearningProfiles { get; set; } = new List<UserLearningProfile>();
}

