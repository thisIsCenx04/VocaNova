using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class Region
{
    public uint RegionId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public uint? ParentId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Region> Inverseparent { get; set; } = new List<Region>();

    public virtual Region? Parent { get; set; }

    public virtual ICollection<UserLearningProfile> UserLearningProfiles { get; set; } = new List<UserLearningProfile>();
}

