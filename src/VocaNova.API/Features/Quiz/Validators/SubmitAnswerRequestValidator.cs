using FluentValidation;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Validators;

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
