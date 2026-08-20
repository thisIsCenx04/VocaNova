using FluentValidation;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.Contracts.Requests;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Phone)
            .VietnamesePhone();

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
