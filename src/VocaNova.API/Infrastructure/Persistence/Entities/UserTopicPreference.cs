using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class UserTopicPreference
{
    public uint UserId { get; set; }

    public uint TopicId { get; set; }

    /// <summary>
    /// knn_suggested/user_selected/onboarding
    /// </summary>
    public string Source { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Topic Topic { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

