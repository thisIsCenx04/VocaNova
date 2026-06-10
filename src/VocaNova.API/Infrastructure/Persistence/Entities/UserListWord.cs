using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class UserListWord
{
    public uint UserId { get; set; }

    public uint ListId { get; set; }

    public uint WordId { get; set; }

    public string AddMethod { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime AddedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual UserList List { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Word Word { get; set; } = null!;
}

