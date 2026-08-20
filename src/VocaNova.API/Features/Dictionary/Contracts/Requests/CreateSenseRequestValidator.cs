using FluentValidation;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class CreateSenseRequestValidator : AbstractValidator<CreateSenseRequest>
{
    public CreateSenseRequestValidator()
    {
        RuleFor(request => request.SenseOrder).GreaterThan(0);
        RuleFor(request => request.WordClass).NotEmpty().MaximumLength(30);
        RuleFor(request => request.EnglishDefinition).NotEmpty();
    }
}
