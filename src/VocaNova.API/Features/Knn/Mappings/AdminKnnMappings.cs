using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.Contracts.Requests;
using VocaNova.API.Features.Knn.Contracts.Responses;

namespace VocaNova.API.Features.Knn.Mappings;

public static class AdminKnnMappings
{
    public static KnnLookupQuery ToBusinessQuery(this KnnLookupRequest request) =>
        new(
            request.Page,
            request.Limit,
            request.Q,
            request.Status,
            request.IncludeDeleted,
            request.SortBy,
            request.SortDirection);

    public static SaveAgeRangeCommand ToBusinessCommand(this CreateAgeRangeRequest request) =>
        new(request.Name, request.MinAge, request.MaxAge, request.DisplayOrder);

    public static SaveAgeRangeCommand ToBusinessCommand(this UpdateAgeRangeRequest request) =>
        new(request.Name, request.MinAge, request.MaxAge, request.DisplayOrder);

    public static SaveRegionCommand ToBusinessCommand(this CreateRegionRequest request) =>
        new(request.Name, request.Code, request.ParentId);

    public static SaveRegionCommand ToBusinessCommand(this UpdateRegionRequest request) =>
        new(request.Name, request.Code, request.ParentId);

    public static SaveOccupationCommand ToBusinessCommand(this CreateOccupationRequest request) =>
        new(request.Name, request.Description);

    public static SaveOccupationCommand ToBusinessCommand(this UpdateOccupationRequest request) =>
        new(request.Name, request.Description);

    public static SaveEducationLevelCommand ToBusinessCommand(this CreateEducationLevelRequest request) =>
        new(request.Name, request.Description, request.DisplayOrder);

    public static SaveEducationLevelCommand ToBusinessCommand(this UpdateEducationLevelRequest request) =>
        new(request.Name, request.Description, request.DisplayOrder);

    public static SaveLearningPurposeCommand ToBusinessCommand(this CreateLearningPurposeRequest request) =>
        new(request.Name, request.Description);

    public static SaveLearningPurposeCommand ToBusinessCommand(this UpdateLearningPurposeRequest request) =>
        new(request.Name, request.Description);

    public static KnnVectorWeights ToBusinessModel(this UpdateKnnVectorWeightsRequest request) =>
        new(
            request.AgeRangeWeight!.Value,
            request.RegionWeight!.Value,
            request.OccupationWeight!.Value,
            request.EducationLevelWeight!.Value,
            request.LearningPurposeWeight!.Value,
            request.InterestTopicsWeight!.Value);

    public static PagedResult<AgeRangeResponse> ToResponse(this PagedResult<AgeRangeLookup> value) =>
        new(value.Items.Select(ToResponse).ToArray(), value.Page, value.Limit, value.TotalItems);

    public static PagedResult<RegionResponse> ToResponse(this PagedResult<RegionLookup> value) =>
        new(value.Items.Select(ToResponse).ToArray(), value.Page, value.Limit, value.TotalItems);

    public static PagedResult<OccupationResponse> ToResponse(this PagedResult<OccupationLookup> value) =>
        new(value.Items.Select(ToResponse).ToArray(), value.Page, value.Limit, value.TotalItems);

    public static PagedResult<EducationLevelResponse> ToResponse(this PagedResult<EducationLevelLookup> value) =>
        new(value.Items.Select(ToResponse).ToArray(), value.Page, value.Limit, value.TotalItems);

    public static PagedResult<LearningPurposeResponse> ToResponse(this PagedResult<LearningPurposeLookup> value) =>
        new(value.Items.Select(ToResponse).ToArray(), value.Page, value.Limit, value.TotalItems);

    public static AgeRangeResponse ToResponse(this AgeRangeLookup value) =>
        new(value.AgeRangeId, value.Name, value.MinAge, value.MaxAge, value.DisplayOrder, value.Status);

    public static RegionResponse ToResponse(this RegionLookup value) =>
        new(value.RegionId, value.Name, value.Code, value.ParentId, value.ParentName, value.Status);

    public static OccupationResponse ToResponse(this OccupationLookup value) =>
        new(value.OccupationId, value.Name, value.Description, value.Status);

    public static EducationLevelResponse ToResponse(this EducationLevelLookup value) =>
        new(value.EducationLevelId, value.Name, value.Description, value.DisplayOrder, value.Status);

    public static LearningPurposeResponse ToResponse(this LearningPurposeLookup value) =>
        new(value.LearningPurposeId, value.Name, value.Description, value.Status);

    public static KnnConfigResponse ToResponse(this KnnConfig value) =>
        new(value.Onboarding.ToResponse(), value.Learning.ToResponse(), value.Vector.ToResponse());

    public static KnnRebuildStatusResponse ToResponse(this KnnRebuildStatus value) =>
        new(value.LastRebuildAt, value.IsRunning);

    public static TriggerKnnRebuildResponse ToResponse(this TriggerKnnRebuildResult value) =>
        new(value.Message, value.TriggeredAt);

    private static KnnOnboardingConfigResponse ToResponse(this KnnOnboardingConfig value) =>
        new(value.KValue, value.DefaultTopicLimit, value.MinSimilarity, value.CacheTtlMinutes);

    private static KnnLearningConfigResponse ToResponse(this KnnLearningConfig value) =>
        new(
            value.KValue,
            value.MinSessions,
            value.MinSimilarity,
            value.RecommendationCount,
            value.RebuildIntervalHours,
            value.CacheTtlMinutes);

    private static KnnVectorConfigResponse ToResponse(this KnnVectorConfig value) =>
        new(value.Weights.ToResponse(), value.Defaults.ToResponse(), value.IsOverridden, value.Storage,
            value.CanWriteEnvFile);

    private static KnnVectorWeightsResponse ToResponse(this KnnVectorWeights value) =>
        new(
            value.AgeRangeWeight,
            value.RegionWeight,
            value.OccupationWeight,
            value.EducationLevelWeight,
            value.LearningPurposeWeight,
            value.InterestTopicsWeight);
}
