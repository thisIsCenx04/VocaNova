using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Services.IServices;

public interface IAdminKnnLookupService
{
    Task<KnnOperationResult<PagedResult<AgeRangeLookup>>> GetAgeRangesAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<AgeRangeLookup>> GetAgeRangeAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<AgeRangeLookup>> CreateAgeRangeAsync(
        SaveAgeRangeCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<AgeRangeLookup>> UpdateAgeRangeAsync(
        uint id,
        SaveAgeRangeCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> DeleteAgeRangeAsync(uint id, CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> RestoreAgeRangeAsync(uint id, CancellationToken cancellationToken = default);

    Task<KnnOperationResult<PagedResult<RegionLookup>>> GetRegionsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<RegionLookup>> GetRegionAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<RegionLookup>> CreateRegionAsync(
        SaveRegionCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<RegionLookup>> UpdateRegionAsync(
        uint id,
        SaveRegionCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> DeleteRegionAsync(uint id, CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> RestoreRegionAsync(uint id, CancellationToken cancellationToken = default);

    Task<KnnOperationResult<PagedResult<OccupationLookup>>> GetOccupationsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<OccupationLookup>> GetOccupationAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<OccupationLookup>> CreateOccupationAsync(
        SaveOccupationCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<OccupationLookup>> UpdateOccupationAsync(
        uint id,
        SaveOccupationCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> DeleteOccupationAsync(uint id, CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> RestoreOccupationAsync(uint id, CancellationToken cancellationToken = default);

    Task<KnnOperationResult<PagedResult<EducationLevelLookup>>> GetEducationLevelsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<EducationLevelLookup>> GetEducationLevelAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<EducationLevelLookup>> CreateEducationLevelAsync(
        SaveEducationLevelCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<EducationLevelLookup>> UpdateEducationLevelAsync(
        uint id,
        SaveEducationLevelCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> DeleteEducationLevelAsync(uint id, CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> RestoreEducationLevelAsync(uint id, CancellationToken cancellationToken = default);

    Task<KnnOperationResult<PagedResult<LearningPurposeLookup>>> GetLearningPurposesAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<LearningPurposeLookup>> GetLearningPurposeAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<LearningPurposeLookup>> CreateLearningPurposeAsync(
        SaveLearningPurposeCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<LearningPurposeLookup>> UpdateLearningPurposeAsync(
        uint id,
        SaveLearningPurposeCommand command,
        CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> DeleteLearningPurposeAsync(uint id, CancellationToken cancellationToken = default);
    Task<KnnOperationResult<bool>> RestoreLearningPurposeAsync(uint id, CancellationToken cancellationToken = default);
}
