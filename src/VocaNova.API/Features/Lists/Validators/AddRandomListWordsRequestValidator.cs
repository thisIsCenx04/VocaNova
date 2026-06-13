using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Validators;

public sealed class AddRandomListWordsRequestValidator : AbstractValidator<AddRandomListWordsRequest>
{
    private static readonly IReadOnlySet<string> SupportedMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        AddMethod.RandomTopic,
        AddMethod.RandomSynonym,
        AddMethod.RandomAntonym,
    };

    public AddRandomListWordsRequestValidator()
    {
        RuleFor(request => request.Count)
            .InclusiveBetween(1, 50);

        RuleFor(request => request.Method)
            .NotEmpty()
            .Must(value => value is not null && SupportedMethods.Contains(value))
            .WithMessage("Method is invalid.");
    }
}
