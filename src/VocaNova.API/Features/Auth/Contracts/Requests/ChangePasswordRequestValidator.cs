using FluentValidation;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.Contracts.Requests;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(request => request.NewPassword)
            .StrongPassword();
    }
}
