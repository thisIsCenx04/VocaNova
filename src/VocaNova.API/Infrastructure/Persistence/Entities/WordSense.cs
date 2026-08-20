using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class WordSense
{
    public uint SenseId { get; set; }

    public uint WordId { get; set; }

    /// <summary>
    /// Thứ tự nghĩa: 1, 2, 3...
    /// </summary>
    public int SenseOrder { get; set; }

    /// <summary>
    /// noun/verb/adjective/adverb...
    /// </summary>
    public string WordClass { get; set; } = null!;

    public string EnglishDefinition { get; set; } = null!;

    public string? VietnameseMeaning { get; set; }

    /// <summary>
    /// active/deleted
    /// </summary>
    public string Status { get; set; } = "active";

    public virtual ICollection<TestAnswer> TestAnswers { get; set; } = new List<TestAnswer>();

    public virtual Word Word { get; set; } = null!;

    public virtual ICollection<WordExample> WordExamples { get; set; } = new List<WordExample>();

    public virtual ICollection<WordRelation> WordRelations { get; set; } = new List<WordRelation>();
}

