using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class User
{
    public uint UserId { get; set; }

    public uint RoleId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<OtpVerification> OtpVerifications { get; set; } = new List<OtpVerification>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<TestSession> TestSessions { get; set; } = new List<TestSession>();

    public virtual UserAuth? UserAuth { get; set; }

    public virtual UserLearningProfile? UserLearningProfile { get; set; }

    public virtual ICollection<UserListWordStat> UserListWordStats { get; set; } = new List<UserListWordStat>();

    public virtual ICollection<UserListWord> UserListWords { get; set; } = new List<UserListWord>();

    public virtual ICollection<UserList> UserLists { get; set; } = new List<UserList>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<UserTopicPreference> UserTopicPreferences { get; set; } = new List<UserTopicPreference>();

    public virtual ICollection<UserWordProgress> UserWordProgresses { get; set; } = new List<UserWordProgress>();
}

