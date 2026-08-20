using FluentValidation;
using VocaNova.API.Features.Auth.Contracts.Requests;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(request => request.IdToken)
            .NotEmpty();
    }
}
