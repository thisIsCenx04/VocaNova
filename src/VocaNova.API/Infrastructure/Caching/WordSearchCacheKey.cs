namespace VocaNova.API.Infrastructure.Caching;

public static class WordSearchCacheKey
{
    public static string Create(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        bool? isPhrase)
    {
        var queryPart = string.IsNullOrWhiteSpace(normalizedQuery) ? "_" : normalizedQuery;
        var cefrPart = string.IsNullOrWhiteSpace(cefr) ? "_" : cefr;
        var topicPart = topicId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "_";
        var phrasePart = isPhrase?.ToString() ?? "_";

        return $"word-search:{queryPart}:{page}:{limit}:{cefrPart}:{topicPart}:{phrasePart}";
    }
}
