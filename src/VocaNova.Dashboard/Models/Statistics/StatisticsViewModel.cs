using VocaNova.Dashboard.Data.Dtos.Stats;

namespace VocaNova.Dashboard.Models.Statistics;

public sealed class StatisticsViewModel
{
    public LearningStats? Learning { get; init; }

    public Demographics? Demographics { get; init; }
}
