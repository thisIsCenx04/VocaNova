using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.Contracts.Requests;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Phone)
            .VietnamesePhone();

        RuleFor(request => request.OtpCode)
            .NotEmpty()
            .Length(AppSettings.OtpCodeLength)
            .Matches("^[0-9]+$");

        RuleFor(request => request.NewPassword)
            .StrongPassword();
    }
}
