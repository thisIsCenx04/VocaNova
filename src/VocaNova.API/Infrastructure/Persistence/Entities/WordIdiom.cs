using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class WordIdiom
{
    public uint IdiomId { get; set; }

    public uint WordId { get; set; }

    public string IdiomText { get; set; } = null!;

    public string? MeaningEn { get; set; }

    public string? MeaningVi { get; set; }

    public virtual Word Word { get; set; } = null!;
}

