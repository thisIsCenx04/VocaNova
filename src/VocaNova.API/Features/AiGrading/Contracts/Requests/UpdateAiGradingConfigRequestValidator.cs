using FluentValidation;

namespace VocaNova.API.Features.AiGrading.Contracts.Requests;

/// <summary>
/// Bounds mirror the clamps the Gemini client already applies, so the admin sees a rejection
/// instead of silently saving a value that would be capped at call time.
/// </summary>
public sealed class UpdateAiGradingConfigRequestValidator
    : AbstractValidator<UpdateAiGradingConfigRequest>
{
    public UpdateAiGradingConfigRequestValidator()
    {
        RuleFor(request => request.Endpoint!)
            .Must(BeAnAbsoluteHttpUrl)
            .WithMessage("Endpoint must be an absolute http(s) URL.")
            .When(request => !string.IsNullOrWhiteSpace(request.Endpoint));

        RuleFor(request => request.Model!)
            .MaximumLength(100)
            .When(request => !string.IsNullOrWhiteSpace(request.Model));

        RuleFor(request => request.FallbackModels!)
            .Must(models => models.Count <= 5)
            .WithMessage("At most 5 fallback models can be configured.")
            .When(request => request.FallbackModels is not null);

        RuleFor(request => request.ApiKey!)
            .MaximumLength(200)
            .When(request => !string.IsNullOrWhiteSpace(request.ApiKey));

        RuleFor(request => request.MaxAttempts!.Value)
            .InclusiveBetween(1, 4)
            .When(request => request.MaxAttempts.HasValue);

        RuleFor(request => request.RetryBaseDelayMs!.Value)
            .InclusiveBetween(0, 5_000)
            .When(request => request.RetryBaseDelayMs.HasValue);

        RuleFor(request => request.AttemptTimeoutSeconds!.Value)
            .InclusiveBetween(1, 15)
            .When(request => request.AttemptTimeoutSeconds.HasValue);

        RuleFor(request => request.PassThreshold!.Value)
            .InclusiveBetween(0.0, 1.0)
            .When(request => request.PassThreshold.HasValue);
    }

    private static bool BeAnAbsoluteHttpUrl(string endpoint)
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
