using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class UserWordProgress
{
    public uint ProgressId { get; set; }

    public uint UserId { get; set; }

    public uint WordId { get; set; }

    public int TestCount { get; set; }

    public int CorrectCount { get; set; }

    public int WrongCount { get; set; }

    public int ConsecutiveCorrect { get; set; }

    public bool IsInWrongList { get; set; }

    public int MasteryLevel { get; set; }

    public int SrsInterval { get; set; }

    public float EaseFactor { get; set; }

    public DateTime? LastTestedAt { get; set; }

    public DateTime? LastWrongAt { get; set; }

    public DateTime? NextReviewAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Word Word { get; set; } = null!;
}

