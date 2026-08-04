using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Validators;

public sealed class AddListWordRequestValidator : AbstractValidator<AddListWordRequest>
{
    public AddListWordRequestValidator()
    {
        RuleFor(request => request.WordId)
            .NotEqual(0u);

        RuleFor(request => request.AddMethod)
            .NotEmpty()
            .Must(value => value is not null && AddMethod.All.Contains(value))
            .WithMessage("AddMethod is invalid.");

        RuleFor(request => request.Note)
            .MaximumLength(1000);
    }
}
