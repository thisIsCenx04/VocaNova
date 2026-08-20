using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.Contracts.Requests;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed class OtpVerifyRequestValidator : AbstractValidator<OtpVerifyRequest>
{
    public OtpVerifyRequestValidator()
    {
        RuleFor(request => request.Phone)
            .VietnamesePhone();

        RuleFor(request => request.OtpCode)
            .NotEmpty()
            .Length(AppSettings.OtpCodeLength)
            .Matches("^[0-9]+$");
    }
}
