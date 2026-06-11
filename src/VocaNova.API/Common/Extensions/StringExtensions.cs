namespace VocaNova.API.Common.Extensions;

public static class StringExtensions
{
    public static string NormalizeWord(this string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public static string NormalizeAnswer(this string value)
    {
        var normalized = value.Trim();

        while (normalized.Length > 0 && char.IsPunctuation(normalized[^1]))
        {
            normalized = normalized[..^1].TrimEnd();
        }

        return normalized.ToLowerInvariant();
    }

    public static string MaskPhone(this string phone)
    {
        var normalized = phone.Trim();

        if (normalized.Length <= 5)
        {
            return new string('*', normalized.Length);
        }

        return $"{normalized[..3]}****{normalized[^2..]}";
    }
}
