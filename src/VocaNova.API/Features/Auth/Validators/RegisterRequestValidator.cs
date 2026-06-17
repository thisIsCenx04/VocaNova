using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Phone)
            .VietnamesePhone();

        RuleFor(request => request.Password)
            .StrongPassword();

        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(request => request.OtpCode)
            .NotEmpty()
            .Length(AppSettings.OtpCodeLength)
            .Matches("^[0-9]+$");
    }
}
