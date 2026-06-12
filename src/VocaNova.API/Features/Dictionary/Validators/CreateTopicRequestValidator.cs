using FluentValidation;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Validators;

public sealed class CreateTopicRequestValidator : AbstractValidator<CreateTopicRequest>
{
    public CreateTopicRequestValidator()
    {
        RuleFor(request => request.TopicName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.TopicNameVi)
            .MaximumLength(50);

        RuleFor(request => request.Icon)
            .MaximumLength(20);
    }
}
