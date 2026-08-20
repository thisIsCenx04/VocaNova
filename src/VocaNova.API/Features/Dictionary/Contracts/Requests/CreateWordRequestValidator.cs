using FluentValidation;
using VocaNova.API.Common.Validation;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class CreateWordRequestValidator : AbstractValidator<CreateWordRequest>
{
    public CreateWordRequestValidator()
    {
        RuleFor(request => request.Word).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Cefr).CefrLevel();
        RuleFor(request => request.PhoneticUk).MaximumLength(100);
        RuleFor(request => request.PhoneticUs).MaximumLength(100);
        RuleFor(request => request.ImageUrl).MaximumLength(500).Must(BeValidUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl))
            .WithMessage("ImageUrl must be a valid absolute URL.");
    }

    private static bool BeValidUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
