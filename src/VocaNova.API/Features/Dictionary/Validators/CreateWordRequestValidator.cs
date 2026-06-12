using FluentValidation;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Validators;

public sealed class CreateWordRequestValidator : AbstractValidator<CreateWordRequest>
{
    public CreateWordRequestValidator()
    {
        RuleFor(request => request.Word)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Cefr)
            .CefrLevel();

        RuleFor(request => request.PhoneticUk)
            .MaximumLength(100);

        RuleFor(request => request.PhoneticUs)
            .MaximumLength(100);

        RuleFor(request => request.ImageUrl)
            .MaximumLength(500)
            .Must(BeValidUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl))
            .WithMessage("ImageUrl must be a valid absolute URL.");
    }

    private static bool BeValidUrl(string? imageUrl)
    {
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
