using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class AiGradingCache
{
    public uint CacheId { get; set; }

    public string CacheKey { get; set; } = null!;

    public uint WordId { get; set; }

    public int QuestionType { get; set; }

    public string UserAnswerNormalized { get; set; } = null!;

    public string ExpectedAnswer { get; set; } = null!;

    public float AiScore { get; set; }

    public string? AiExplanation { get; set; }

    public string? AiSuggestion { get; set; }

    public int HitCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public virtual Word Word { get; set; } = null!;
}

