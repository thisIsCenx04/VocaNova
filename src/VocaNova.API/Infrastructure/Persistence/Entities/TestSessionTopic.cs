namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class TestSessionTopic
{
    public uint SessionId { get; set; }

    public uint TopicId { get; set; }

    public virtual TestSession Session { get; set; } = null!;

    public virtual Topic Topic { get; set; } = null!;
}

