using FluentValidation;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Validators;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
