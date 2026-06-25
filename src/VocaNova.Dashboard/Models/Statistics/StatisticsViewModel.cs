using VocaNova.Dashboard.Models.Api.Stats;

namespace VocaNova.Dashboard.Models.Statistics;

public sealed class StatisticsViewModel
{
    public LearningStats? Learning { get; init; }

    public Demographics? Demographics { get; init; }
}
