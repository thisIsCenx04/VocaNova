namespace VocaNova.API.Features.Progress.BLL.Models;

public sealed record ProgressSummaryQuery(
    DateTime SevenDayStartInclusive,
    DateTime TomorrowExclusive,
    DateTime MonthStartInclusive,
    int MasteredLevel);
