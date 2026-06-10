using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class WordRelation
{
    public uint RelationId { get; set; }

    public uint WordId { get; set; }

    /// <summary>
    /// null = áp dụng cho toàn bộ từ
    /// </summary>
    public uint? SenseId { get; set; }

    /// <summary>
    /// synonym/antonym
    /// </summary>
    public string RelationType { get; set; } = null!;

    public string RelatedWord { get; set; } = null!;

    public uint? RelatedWordId { get; set; }

    public bool? IsQuizEligible { get; set; }

    public virtual Word? RelatedWordNavigation { get; set; }

    public virtual WordSense? Sense { get; set; }

    public virtual Word Word { get; set; } = null!;
}

