using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class UserAuth
{
    public uint UserId { get; set; }

    public string? Phone { get; set; }

    public bool IsPhoneVerified { get; set; }

    public string? GoogleUid { get; set; }

    public string? GoogleEmail { get; set; }

    public string? Username { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

