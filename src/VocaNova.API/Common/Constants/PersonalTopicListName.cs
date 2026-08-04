using System.Globalization;

namespace VocaNova.API.Common.Constants;

public static class PersonalTopicListName
{
    public const string Prefix = "__topic__:";

    public static string For(uint topicId) => $"{Prefix}{topicId}";

    public static bool IsReserved(string? listName)
    {
        return listName?.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool TryGetTopicId(string? listName, out uint topicId)
    {
        topicId = 0;
        if (!IsReserved(listName))
        {
            return false;
        }

        return uint.TryParse(
            listName![Prefix.Length..],
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out topicId)
            && topicId > 0;
    }
}
