using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class Word
{
    public uint WordId { get; set; }

    /// <summary>
    /// Từ tiếng Anh — lowercase normalized
    /// </summary>
    public string Word1 { get; set; } = null!;

    /// <summary>
    /// casefold+strip — lookup tránh duplicate
    /// </summary>
    public string WordKey { get; set; } = null!;

    /// <summary>
    /// A1/A2/B1/B2/C1/C2
    /// </summary>
    public string? CefrLevel { get; set; }

    public string? PhoneticUk { get; set; }

    public string? PhoneticUs { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>
    /// 1=idiom/phrase/compound
    /// </summary>
    public bool IsPhrase { get; set; }

    /// <summary>
    /// active/deleted
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AiGradingCache> AiGradingCaches { get; set; } = new List<AiGradingCache>();

    public virtual ICollection<TestAnswer> TestAnswers { get; set; } = new List<TestAnswer>();

    public virtual ICollection<UserListWordStat> UserListWordStats { get; set; } = new List<UserListWordStat>();

    public virtual ICollection<UserListWord> UserListWords { get; set; } = new List<UserListWord>();

    public virtual ICollection<UserWordProgress> UserWordProgresses { get; set; } = new List<UserWordProgress>();

    public virtual ICollection<WordAudioAsset> WordAudioAssets { get; set; } = new List<WordAudioAsset>();

    public virtual ICollection<WordDerivedForm> WordDerivedFormderivedWordNavigations { get; set; } = new List<WordDerivedForm>();

    public virtual ICollection<WordDerivedForm> WordDerivedFormwords { get; set; } = new List<WordDerivedForm>();

    public virtual ICollection<WordExample> WordExamples { get; set; } = new List<WordExample>();

    public virtual ICollection<WordIdiom> WordIdioms { get; set; } = new List<WordIdiom>();

    public virtual ICollection<WordRelation> WordRelationrelatedWordNavigations { get; set; } = new List<WordRelation>();

    public virtual ICollection<WordRelation> WordRelationwords { get; set; } = new List<WordRelation>();

    public virtual ICollection<WordSense> WordSenses { get; set; } = new List<WordSense>();

    public virtual ICollection<WordTopic> WordTopics { get; set; } = new List<WordTopic>();
}

