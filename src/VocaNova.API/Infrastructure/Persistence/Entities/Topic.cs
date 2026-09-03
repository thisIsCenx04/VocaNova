using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class Topic
{
    public uint TopicId { get; set; }

    public string TopicName { get; set; } = null!;

    public string? TopicNameVi { get; set; }

    public string? Icon { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<TestSessionTopic> TestSessionTopics { get; set; } = new List<TestSessionTopic>();

    public virtual ICollection<UserTopicPreference> UserTopicPreferences { get; set; } = new List<UserTopicPreference>();

    public virtual ICollection<WordTopic> WordTopics { get; set; } = new List<WordTopic>();
}
