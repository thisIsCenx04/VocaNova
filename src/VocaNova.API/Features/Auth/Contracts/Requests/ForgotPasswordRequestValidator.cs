using FluentValidation;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.Contracts.Requests;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(request => request.Phone)
            .VietnamesePhone();
    }
}
