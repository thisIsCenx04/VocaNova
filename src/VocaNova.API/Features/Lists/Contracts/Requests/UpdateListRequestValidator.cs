using FluentValidation;
using VocaNova.API.Features.Lists.Contracts.Requests;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed class UpdateListRequestValidator : AbstractValidator<UpdateListRequest>
{
    public UpdateListRequestValidator()
    {
        RuleFor(request => request.ListName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
