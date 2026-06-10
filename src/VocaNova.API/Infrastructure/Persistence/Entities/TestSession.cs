using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class TestSession
{
    public uint SessionId { get; set; }

    public uint UserId { get; set; }

    public string TestType { get; set; } = null!;

    public string Mode { get; set; } = null!;

    public int QuestionType { get; set; }

    public string ScopeType { get; set; } = null!;

    public DateOnly? ScopeDateFrom { get; set; }

    public DateOnly? ScopeDateTo { get; set; }

    public string WordOrder { get; set; } = null!;

    public int? WordLimit { get; set; }

    public int? TimeLimitSec { get; set; }

    public int? Lives { get; set; }

    public int QuestionCount { get; set; }

    public int CorrectCount { get; set; }

    public int WrongCount { get; set; }

    public float Score { get; set; }

    public int MaxStreak { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<TestAnswer> TestAnswers { get; set; } = new List<TestAnswer>();

    public virtual ICollection<TestSessionTopic> TestSessionTopics { get; set; } = new List<TestSessionTopic>();

    public virtual User User { get; set; } = null!;
}

