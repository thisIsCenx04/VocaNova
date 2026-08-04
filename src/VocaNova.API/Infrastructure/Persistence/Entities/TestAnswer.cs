using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class TestAnswer
{
    public uint AnswerId { get; set; }

    public uint SessionId { get; set; }

    public uint WordId { get; set; }

    public uint? SenseId { get; set; }

    public int QuestionNumber { get; set; }

    public int QuestionType { get; set; }

    public string DisplayContent { get; set; } = null!;

    public string ExpectedAnswer { get; set; } = null!;

    public string? AcceptedAnswers { get; set; }

    public string? UserAnswer { get; set; }

    public bool? IsCorrect { get; set; }

    public float? AiScore { get; set; }

    public string? AiExplanation { get; set; }

    public string? AiSuggestion { get; set; }

    public virtual WordSense? Sense { get; set; }

    public virtual TestSession Session { get; set; } = null!;

    public virtual Word Word { get; set; } = null!;
}

