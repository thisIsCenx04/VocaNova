using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class WordDerivedForm
{
    public uint DerivedId { get; set; }

    public uint WordId { get; set; }

    public string DerivedWord { get; set; } = null!;

    public uint? DerivedWordId { get; set; }

    public string? WordClass { get; set; }

    public virtual Word? DerivedWordNavigation { get; set; }

    public virtual Word Word { get; set; } = null!;
}

