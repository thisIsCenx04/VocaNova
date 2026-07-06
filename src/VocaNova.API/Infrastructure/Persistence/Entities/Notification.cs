using System;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class Notification
{
    public uint NotificationId { get; set; }

    public uint UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? RefType { get; set; }

    public uint? RefId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
