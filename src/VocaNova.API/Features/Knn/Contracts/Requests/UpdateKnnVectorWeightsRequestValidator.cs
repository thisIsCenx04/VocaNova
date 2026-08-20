using System.Linq.Expressions;
using FluentValidation;

namespace VocaNova.API.Features.Knn.Contracts.Requests;

public sealed class UpdateKnnVectorWeightsRequestValidator : AbstractValidator<UpdateKnnVectorWeightsRequest>
{
    private const double MaxWeight = 10.0;

    public UpdateKnnVectorWeightsRequestValidator()
    {
        Weight(request => request.AgeRangeWeight, nameof(UpdateKnnVectorWeightsRequest.AgeRangeWeight));
        Weight(request => request.RegionWeight, nameof(UpdateKnnVectorWeightsRequest.RegionWeight));
        Weight(request => request.OccupationWeight, nameof(UpdateKnnVectorWeightsRequest.OccupationWeight));
        Weight(request => request.EducationLevelWeight, nameof(UpdateKnnVectorWeightsRequest.EducationLevelWeight));
        Weight(request => request.LearningPurposeWeight, nameof(UpdateKnnVectorWeightsRequest.LearningPurposeWeight));
        Weight(request => request.InterestTopicsWeight, nameof(UpdateKnnVectorWeightsRequest.InterestTopicsWeight));

        RuleFor(request => request)
            .Must(HasANonZeroWeight)
            .WithName("Weights")
            .WithMessage("At least one weight must be greater than zero.");
    }

    private void Weight(Expression<Func<UpdateKnnVectorWeightsRequest, double?>> selector, string name)
    {
        RuleFor(selector)
            .NotNull()
            .WithMessage($"{name} is required.")
            .DependentRules(() =>
            {
                RuleFor(selector)
                    .Must(value => value >= 0 && value <= MaxWeight)
                    .WithMessage($"{name} must be between 0 and {MaxWeight}.");
            });
    }

    private static bool HasANonZeroWeight(UpdateKnnVectorWeightsRequest request) =>
        new[]
        {
            request.AgeRangeWeight,
            request.RegionWeight,
            request.OccupationWeight,
            request.EducationLevelWeight,
            request.LearningPurposeWeight,
            request.InterestTopicsWeight,
        }.Any(weight => weight is > 0);
}
