using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class OtpVerification
{
    public uint OtpId { get; set; }

    public uint? UserId { get; set; }

    public string Phone { get; set; } = null!;

    public string OtpCode { get; set; } = null!;

    public bool IsUsed { get; set; }

    public string Status { get; set; } = null!;

    public int VerifyAttemptCount { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}

