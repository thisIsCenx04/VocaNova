using System.Diagnostics;
using System.Text.Json;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.AiGrading.BLL.Abstractions;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.BLL.Services;
using VocaNova.API.Features.AiGrading.BLL.Services.IServices;

namespace VocaNova.API.Infrastructure.ExternalServices.Gemini;

public sealed class GeminiAiGradingProvider : IAiGradingProvider
{
    private const string UnavailableExplanation = "AI không khả dụng";
    private const string FallbackMatchExplanation = "Chấm tự động: khớp chính xác (AI tạm thời không khả dụng).";
    private const string TestPrompt = """
        Return only valid JSON: {"score": 1.0, "explanation": "ok", "suggestion": null}
        """;
    private static readonly TimeSpan GradingTimeout = TimeSpan.FromSeconds(18);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IGeminiClient _client;
    private readonly IAiGradingConfigurationService? _configurationService;
    private readonly ILogger<GeminiAiGradingProvider> _logger;

    public GeminiAiGradingProvider(IGeminiClient client, ILogger<GeminiAiGradingProvider> logger,
        IAiGradingConfigurationService? configurationService = null)
    {
        _client = client;
        _logger = logger;
        _configurationService = configurationService;
    }

    public async Task<AiGrade> GradeAsync(AiGradeRequest request,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(GradingTimeout);
        try
        {
            var response = await _client.GenerateContentAsync(BuildPrompt(request), timeout.Token);
            var parsed = ParseResponse(response);
            if (parsed is null || parsed.Score is < 0 or > 1) return CreateFallback(request);
            var threshold = _configurationService is null
                ? Common.Constants.AppSettings.AiPassThreshold
                : (await _configurationService.GetEffectiveSettingsAsync(timeout.Token)).PassThreshold;
            return new AiGrade(parsed.Score >= threshold, parsed.Score, parsed.Explanation, parsed.Suggestion);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Gemini AI grading failed; using fallback grading.");
            return CreateFallback(request);
        }
    }

    public async Task<AiGradingConnectionTest> TestConnectionAsync(
        AiGradingConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            return new AiGradingConnectionTest(false, configuration.Model, 0, "No API key is configured.");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _client.GenerateContentAsync(TestPrompt, configuration, cancellationToken);
            stopwatch.Stop();
            return new AiGradingConnectionTest(true, configuration.Model, stopwatch.ElapsedMilliseconds,
                "The provider responded successfully.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new AiGradingConnectionTest(false, configuration.Model, stopwatch.ElapsedMilliseconds,
                exception.Message);
        }
    }

    private static string BuildPrompt(AiGradeRequest request) => $$"""
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
        word_id: {{request.WordId}}
        question_type: {{request.QuestionType}}
        expected_answer: {{request.ExpectedAnswer}}
        user_answer: {{request.UserAnswer ?? string.Empty}}
        """;

    private static GeminiGradingResponse? ParseResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText)) return null;
        return JsonSerializer.Deserialize<GeminiGradingResponse>(
            StripJsonCodeFence(responseText.Trim()), JsonOptions);
    }

    private static string StripJsonCodeFence(string value)
    {
        if (!value.StartsWith("```", StringComparison.Ordinal)) return value;
        var firstNewLine = value.IndexOf('\n');
        var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine < 0 || lastFence <= firstNewLine
            ? value
            : value[(firstNewLine + 1)..lastFence].Trim();
    }

    private static AiGrade CreateFallback(AiGradeRequest request)
    {
        var exact = !string.IsNullOrWhiteSpace(request.UserAnswer)
            && string.Equals(request.UserAnswer.NormalizeAnswer(),
                request.ExpectedAnswer.NormalizeAnswer(), StringComparison.Ordinal);
        return exact
            ? new AiGrade(true, 1f, FallbackMatchExplanation, null, FromAi: false)
            : new AiGrade(false, 0f, UnavailableExplanation, null, FromAi: false);
    }
}
