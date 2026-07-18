using System.Text.Json;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.AiGrading.DTOs;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.AiGrading.Services;

public sealed class GeminiAiGradingProvider : IAiGradingProvider
{
    private const string UnavailableExplanation = "AI không khả dụng";
    private const string FallbackMatchExplanation = "Chấm tự động: khớp chính xác (AI tạm thời không khả dụng).";

    // Bound the Gemini call server-side so a slow provider can't hang until the
    // mobile client aborts the request (which surfaces as an unhandled 500).
    private static readonly TimeSpan GradingTimeout = TimeSpan.FromSeconds(18);
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
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(GradingTimeout);
        try
        {
            var prompt = BuildPrompt(wordId, questionType, userAnswer, expectedAnswer);
            var responseText = await _geminiClient.GenerateContentAsync(prompt, timeoutCts.Token);
            var parsed = ParseResponse(responseText);
            if (parsed is null || parsed.Score is < 0 or > 1)
            {
                return CreateFallback(userAnswer, expectedAnswer);
            }

            return new AiGradingResult(
                parsed.Score >= AppSettings.AiPassThreshold,
                parsed.Score,
                parsed.Explanation,
                parsed.Suggestion);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The client aborted the request; there is nobody to grade for.
            throw;
        }
        catch (Exception exception)
        {
            // Our own grading timeout or any Gemini/transport failure: degrade
            // gracefully instead of failing the whole answer submission.
            _logger.LogWarning(exception, "Gemini AI grading failed; using fallback grading.");
            return CreateFallback(userAnswer, expectedAnswer);
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

    private static AiGradingResult CreateFallback(string? userAnswer, string expectedAnswer)
    {
        // When AI is unavailable, fall back to an exact (normalized) match so a
        // correct answer is not wrongly marked incorrect. FromAi = false keeps
        // this temporary result out of the grading cache.
        var isExactMatch = !string.IsNullOrWhiteSpace(userAnswer)
            && string.Equals(
                userAnswer.NormalizeAnswer(),
                expectedAnswer.NormalizeAnswer(),
                StringComparison.Ordinal);

        return isExactMatch
            ? new AiGradingResult(true, 1f, FallbackMatchExplanation, null, FromAi: false)
            : new AiGradingResult(false, 0f, UnavailableExplanation, null, FromAi: false);
    }
}
