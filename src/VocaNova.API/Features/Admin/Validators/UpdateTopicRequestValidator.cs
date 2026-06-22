using FluentValidation;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Admin.Validators;

public sealed class UpdateTopicRequestValidator : AbstractValidator<UpdateTopicRequest>
{
    public UpdateTopicRequestValidator()
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
