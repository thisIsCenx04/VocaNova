using System.Text.Json;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.AiGrading.DTOs;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.AiGrading.Services;

public sealed class GeminiAiGradingProvider : IAiGradingProvider
{
    private const string UnavailableExplanation = "AI không khả dụng";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IGeminiClient _geminiClient;
    private readonly ILogger<GeminiAiGradingProvider> _logger;

    public GeminiAiGradingProvider(
        IGeminiClient geminiClient,
        ILogger<GeminiAiGradingProvider> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
    }

    public async Task<AiGradingResult> GradeAsync(
        uint wordId,
        int questionType,
        string? userAnswer,
        string expectedAnswer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildPrompt(wordId, questionType, userAnswer, expectedAnswer);
            var responseText = await _geminiClient.GenerateContentAsync(prompt, cancellationToken);
            var parsed = ParseResponse(responseText);
            if (parsed is null || parsed.Score is < 0 or > 1)
            {
                return CreateFallback();
            }

            return new AiGradingResult(
                parsed.Score >= AppSettings.AiPassThreshold,
                parsed.Score,
                parsed.Explanation,
                parsed.Suggestion);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Gemini AI grading failed.");
            return CreateFallback();
        }
    }

    private static string BuildPrompt(
        uint wordId,
        int questionType,
        string? userAnswer,
        string expectedAnswer)
    {
        return $$"""
            You are grading an English vocabulary quiz answer.
            Return only valid JSON with exactly these fields:
            {
              "score": number between 0.0 and 1.0,
              "explanation": string,
              "suggestion": string or null
            }

            Grading rules:
            - Award 1.0 when the answer is fully correct.
            - Award partial credit for semantically close answers.
            - Use concise Vietnamese for explanation and suggestion.

            Context:
            word_id: {{wordId}}
            question_type: {{questionType}}
            expected_answer: {{expectedAnswer}}
            user_answer: {{userAnswer ?? string.Empty}}
            """;
    }

    private static GeminiGradingResponseDto? ParseResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        var payload = StripJsonCodeFence(responseText.Trim());
        return JsonSerializer.Deserialize<GeminiGradingResponseDto>(payload, JsonOptions);
    }

    private static string StripJsonCodeFence(string value)
    {
        if (!value.StartsWith("```", StringComparison.Ordinal))
        {
            return value;
        }

        var firstNewLine = value.IndexOf('\n');
        var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);
        if (firstNewLine < 0 || lastFence <= firstNewLine)
        {
            return value;
        }

        return value[(firstNewLine + 1)..lastFence].Trim();
    }

    private static AiGradingResult CreateFallback()
    {
        return new AiGradingResult(false, 0f, UnavailableExplanation, null);
    }
}
