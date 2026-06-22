using FluentValidation;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Admin.Validators;

public sealed class UpdateSenseRequestValidator : AbstractValidator<UpdateSenseRequest>
{
    public UpdateSenseRequestValidator()
    {
        RuleFor(request => request.SenseOrder)
            .GreaterThan(0);

        RuleFor(request => request.WordClass)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(request => request.EnglishDefinition)
            .NotEmpty();
    }
}
