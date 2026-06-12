using FluentValidation;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Validators;

public sealed class UpdateListRequestValidator : AbstractValidator<UpdateListRequest>
{
    public UpdateListRequestValidator()
    {
        RuleFor(request => request.ListName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
