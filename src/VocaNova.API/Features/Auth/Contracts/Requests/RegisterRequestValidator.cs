using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.Auth.Contracts.Requests;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

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

        // Learning-profile fields below are optional: they seed the KNN profile vector at
        // sign-up so a brand-new user is not stuck without recommendations, but leaving them
        // blank must never block registration.
        RuleFor(request => request.DateOfBirth!.Value)
            .Must(BeAPlausibleAge)
            .WithMessage(
                $"DateOfBirth must correspond to an age between {AppSettings.MinRegistrationAge} "
                + $"and {AppSettings.MaxRegistrationAge}.")
            .When(request => request.DateOfBirth.HasValue);

        RuleFor(request => request.RegionId!.Value)
            .GreaterThan(0u)
            .When(request => request.RegionId.HasValue);

        RuleFor(request => request.OccupationId!.Value)
            .GreaterThan(0u)
            .When(request => request.OccupationId.HasValue);

        RuleFor(request => request.EducationLevelId!.Value)
            .GreaterThan(0u)
            .When(request => request.EducationLevelId.HasValue);
    }

    private static bool BeAPlausibleAge(DateOnly dateOfBirth)
    {
        var age = AgeHelper.CalculateAge(dateOfBirth);
        return age >= AppSettings.MinRegistrationAge && age <= AppSettings.MaxRegistrationAge;
    }
}
