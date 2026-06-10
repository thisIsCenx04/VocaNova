using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class UserList
{
    public uint ListId { get; set; }

    public uint UserId { get; set; }

    public string ListName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserListWordStat> UserListWordStats { get; set; } = new List<UserListWordStat>();

    public virtual ICollection<UserListWord> UserListWords { get; set; } = new List<UserListWord>();
}

