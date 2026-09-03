using FluentValidation;
using VocaNova.API.Features.Lists.Contracts.Requests;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed class AddPersonalTopicWordRequestValidator : AbstractValidator<AddPersonalTopicWordRequest>
{
    public AddPersonalTopicWordRequestValidator()
    {
        RuleFor(request => request.WordId).NotEqual(0u);
        RuleFor(request => request.Note).MaximumLength(1000);
    }
}
