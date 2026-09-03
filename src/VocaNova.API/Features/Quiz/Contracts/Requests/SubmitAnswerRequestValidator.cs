using FluentValidation;

namespace VocaNova.API.Features.Quiz.Contracts.Requests;

public sealed class SubmitAnswerRequestValidator : AbstractValidator<SubmitAnswerRequest>
{
    public SubmitAnswerRequestValidator()
    {
        RuleFor(request => request.WordId)
            .Must(wordId => wordId > 0)
            .WithMessage("WordId must be greater than 0.");

        RuleFor(request => request.UserAnswer)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
