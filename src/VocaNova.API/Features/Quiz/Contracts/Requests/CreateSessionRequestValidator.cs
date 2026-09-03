using FluentValidation;
using VocaNova.API.Common.Constants;

namespace VocaNova.API.Features.Quiz.Contracts.Requests;

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    private static readonly IReadOnlySet<int> QuestionTypes = new HashSet<int>
    {
        int.Parse(QuestionType.WordToMeaning),
        int.Parse(QuestionType.MeaningToWord),
        int.Parse(QuestionType.Description),
    };

    public CreateSessionRequestValidator()
    {
        RuleFor(request => request.Mode)
            .NotEmpty()
            .Must(value => value is not null && TestMode.All.Contains(value))
            .WithMessage("Mode is invalid.");

        RuleFor(request => request.QuestionType)
            .Must(QuestionTypes.Contains)
            .WithMessage("Question type is invalid.");

        RuleFor(request => request.ScopeType)
            .NotEmpty()
            .Must(value => value is not null && ScopeType.Values.Contains(value))
            .WithMessage("Scope type is invalid.");

        RuleFor(request => request.WordOrder)
            .NotEmpty()
            .Must(value => value is not null && WordOrder.All.Contains(value))
            .WithMessage("Word order is invalid.");

        RuleFor(request => request.AnswerMethod)
            .NotEmpty()
            .Must(value => value is not null && AnswerMethod.All.Contains(value))
            .WithMessage("Answer method is invalid.");

        RuleFor(request => request.WordLimit)
            .GreaterThan(0)
            .When(request => request.WordLimit.HasValue);

        RuleFor(request => request.TimeLimitSec)
            .GreaterThan(0)
            .When(request => request.TimeLimitSec.HasValue);

        RuleFor(request => request.Lives)
            .GreaterThan(0)
            .When(request => request.Lives.HasValue);

        RuleFor(request => request.TimeLimitSec)
            .NotNull()
            .When(request => request.Mode == TestMode.Timed)
            .WithMessage("Timed mode requires time_limit_sec.");

        RuleFor(request => request.Lives)
            .NotNull()
            .When(request => request.Mode == TestMode.Elimination)
            .WithMessage("Elimination mode requires lives.");

        RuleFor(request => request.ScopeDateFrom)
            .NotNull()
            .When(request => request.ScopeType is ScopeType.DateRange or ScopeType.StartDate)
            .WithMessage("scope_date_from is required.");

        RuleFor(request => request.ScopeDateTo)
            .NotNull()
            .When(request => request.ScopeType is ScopeType.DateRange or ScopeType.EndDate)
            .WithMessage("scope_date_to is required.");

        RuleFor(request => request)
            .Must(request => !request.ScopeDateFrom.HasValue
                || !request.ScopeDateTo.HasValue
                || request.ScopeDateFrom.Value <= request.ScopeDateTo.Value)
            .WithMessage("scope_date_from must be before or equal to scope_date_to.");
    }
}
