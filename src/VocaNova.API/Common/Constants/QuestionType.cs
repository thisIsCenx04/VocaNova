namespace VocaNova.API.Common.Constants;

public static class QuestionType
{
    public const string WordToMeaning = "1";
    public const string MeaningToWord = "2";
    public const string Description = "3";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WordToMeaning,
        MeaningToWord,
        Description,
    };
}
