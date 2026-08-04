using FluentValidation;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Validators;

public sealed class UpdateLearningProfileRequestValidator : AbstractValidator<UpdateLearningProfileRequest>
{
    public UpdateLearningProfileRequestValidator()
    {
        RuleFor(request => request.AgeRangeId)
            .Must(BeNullOrPositive);

        RuleFor(request => request.RegionId)
            .Must(BeNullOrPositive);

        RuleFor(request => request.OccupationId)
            .Must(BeNullOrPositive);

        RuleFor(request => request.EducationLevelId)
            .Must(BeNullOrPositive);

        RuleFor(request => request.LearningPurposeId)
            .Must(BeNullOrPositive);
    }

    private static bool BeNullOrPositive(uint? value)
    {
        return !value.HasValue || value.Value > 0;
    }
}
