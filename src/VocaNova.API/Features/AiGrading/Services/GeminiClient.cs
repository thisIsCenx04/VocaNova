using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.AiGrading.DTOs;

namespace VocaNova.API.Features.AiGrading.Services;

public sealed class GeminiClient : IGeminiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AiGradingSettings _settings;

    public GeminiClient(
        HttpClient httpClient,
        IOptions<AiGradingSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.Model))
        {
            throw new InvalidOperationException("Gemini configuration is missing.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{NormalizeModelName(_settings.Model)}:generateContent");
        request.Headers.Add("x-goog-api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(CreateRequestBody(prompt), options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

        return ExtractText(json.RootElement);
    }

    private static object CreateRequestBody(string prompt)
    {
        return new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt },
                    },
                },
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json",
            },
        };
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response has no candidates.");
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response has no text parts.");
        }

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text)
                && text.ValueKind == JsonValueKind.String)
            {
                return text.GetString() ?? string.Empty;
            }
        }

        throw new InvalidOperationException("Gemini response text is missing.");
    }

    private static string NormalizeModelName(string model)
    {
        var normalized = model.Trim();
        if (normalized.StartsWith("models/", StringComparison.Ordinal))
        {
            return $"models/{Uri.EscapeDataString(normalized["models/".Length..])}";
        }

        return $"models/{Uri.EscapeDataString(normalized)}";
    }
}
