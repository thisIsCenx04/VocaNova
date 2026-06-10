using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class WordExample
{
    public uint ExampleId { get; set; }

    public uint WordId { get; set; }

    /// <summary>
    /// null = ví dụ chung cho cả từ
    /// </summary>
    public uint? SenseId { get; set; }

    public string ExampleEn { get; set; } = null!;

    public string? ExampleVi { get; set; }

    public int OrderIndex { get; set; }

    public virtual WordSense? Sense { get; set; }

    public virtual Word Word { get; set; } = null!;
}

