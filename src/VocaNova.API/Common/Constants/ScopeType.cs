namespace VocaNova.API.Common.Constants;

public static class ScopeType
{
    public const string All = "all";
    public const string DateRange = "date_range";
    public const string StartDate = "start_date";
    public const string EndDate = "end_date";
    public const string WrongWords = "wrong_words";

    public static readonly IReadOnlySet<string> Values = new HashSet<string>(StringComparer.Ordinal)
    {
        All,
        DateRange,
        StartDate,
        EndDate,
        WrongWords,
    };
}
