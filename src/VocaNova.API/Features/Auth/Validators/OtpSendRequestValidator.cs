using FluentValidation;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Validators;

public sealed class OtpSendRequestValidator : AbstractValidator<OtpSendRequest>
{
    public OtpSendRequestValidator()
    {
        RuleFor(request => request.Phone)
            .VietnamesePhone();

        RuleFor(request => request.Purpose)
            .MaximumLength(20)
            .When(request => !string.IsNullOrWhiteSpace(request.Purpose));
    }
}
