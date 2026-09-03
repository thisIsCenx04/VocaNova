using FluentValidation;
using VocaNova.API.Features.Lists.Contracts.Requests;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed class CreateListRequestValidator : AbstractValidator<CreateListRequest>
{
    public CreateListRequestValidator()
    {
        RuleFor(request => request.ListName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
