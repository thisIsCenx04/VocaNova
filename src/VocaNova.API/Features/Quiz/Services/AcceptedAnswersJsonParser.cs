using System.Text.Json;

namespace VocaNova.API.Features.Quiz.Services;

public static class AcceptedAnswersJsonParser
{
    public static IReadOnlyCollection<string> Parse(string? acceptedAnswersJson)
    {
        if (string.IsNullOrWhiteSpace(acceptedAnswersJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            var answers = JsonSerializer.Deserialize<string[]>(acceptedAnswersJson);
            return answers?
                .Where(answer => !string.IsNullOrWhiteSpace(answer))
                .Select(answer => answer.Trim())
                .ToArray()
                ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
