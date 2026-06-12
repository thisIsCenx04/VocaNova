using FluentValidation;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Validators;

public sealed class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(request => request.AvatarUrl)
            .MaximumLength(500)
            .Must(BeValidUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.AvatarUrl))
            .WithMessage("AvatarUrl must be a valid absolute URL.");
    }

    private static bool BeValidUrl(string? avatarUrl)
    {
        return Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
