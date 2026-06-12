using FluentValidation;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Validators;

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(request => request.IdToken)
            .NotEmpty();
    }
}
