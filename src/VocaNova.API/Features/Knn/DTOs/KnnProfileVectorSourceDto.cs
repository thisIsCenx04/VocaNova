namespace VocaNova.API.Features.Knn.DTOs;

/// <summary>
/// Raw material for the hybrid profile vector: the demographic block comes from the sign-up
/// form, the intent block (learning purpose + interest topics) from the onboarding questions.
/// Every part is optional, so a partially filled profile still yields a usable vector.
/// </summary>
public sealed record KnnProfileVectorSourceDto(
    uint UserId,
    uint? AgeRangeId,
    uint? RegionId,
    uint? OccupationId,
    uint? EducationLevelId,
    uint? LearningPurposeId,
    IReadOnlyCollection<uint>? InterestTopicIds = null);
