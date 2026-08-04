using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class UserListWordStat
{
    public uint UserId { get; set; }

    public uint ListId { get; set; }

    public uint WordId { get; set; }

    public int CorrectCount { get; set; }

    public int WrongCount { get; set; }

    public DateTime? LastTestedAt { get; set; }

    public virtual UserList List { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Word Word { get; set; } = null!;
}

