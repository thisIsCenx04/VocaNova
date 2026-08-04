using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class WordAudioAsset
{
    public uint AudioId { get; set; }

    public uint WordId { get; set; }

    /// <summary>
    /// uk/us
    /// </summary>
    public string Accent { get; set; } = null!;

    /// <summary>
    /// original/tts/uploaded
    /// </summary>
    public string Source { get; set; } = null!;

    public string? StorageUrl { get; set; }

    /// <summary>
    /// pending/uploaded/missing/tts_generated
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Word Word { get; set; } = null!;
}

