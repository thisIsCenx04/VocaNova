using FluentValidation;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Validators;

public sealed class CreateListRequestValidator : AbstractValidator<CreateListRequest>
{
    public CreateListRequestValidator()
    {
        RuleFor(request => request.ListName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
