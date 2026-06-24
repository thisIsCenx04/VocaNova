using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class WordTopic
{
    public uint WordId { get; set; }

    public uint TopicId { get; set; }

    public virtual Topic Topic { get; set; } = null!;

    public virtual Word Word { get; set; } = null!;
}

