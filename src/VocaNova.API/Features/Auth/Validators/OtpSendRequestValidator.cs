using FluentValidation;
using VocaNova.API.Common.Constants;
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
            .Must(purpose => string.IsNullOrWhiteSpace(purpose)
                || OtpPurpose.All.Contains(purpose.Trim().ToLowerInvariant()))
            .WithMessage("Purpose must be register, verify, or reset.");
    }
}
