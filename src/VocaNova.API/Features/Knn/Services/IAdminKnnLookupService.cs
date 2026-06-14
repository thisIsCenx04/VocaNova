using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Features.Knn.Services;

public interface IAdminKnnLookupService
{
    Task<Result<PagedResult<AgeRangeDto>>> GetAgeRangesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<Result<AgeRangeDto>> GetAgeRangeAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default);
    Task<Result<AgeRangeDto>> CreateAgeRangeAsync(CreateAgeRangeRequest request, CancellationToken cancellationToken = default);
    Task<Result<AgeRangeDto>> UpdateAgeRangeAsync(uint id, UpdateAgeRangeRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAgeRangeAsync(uint id, CancellationToken cancellationToken = default);
    Task<Result<bool>> RestoreAgeRangeAsync(uint id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<RegionDto>>> GetRegionsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<Result<RegionDto>> GetRegionAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default);
    Task<Result<RegionDto>> CreateRegionAsync(CreateRegionRequest request, CancellationToken cancellationToken = default);
    Task<Result<RegionDto>> UpdateRegionAsync(uint id, UpdateRegionRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteRegionAsync(uint id, CancellationToken cancellationToken = default);
    Task<Result<bool>> RestoreRegionAsync(uint id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<OccupationDto>>> GetOccupationsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<Result<OccupationDto>> GetOccupationAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default);
    Task<Result<OccupationDto>> CreateOccupationAsync(CreateOccupationRequest request, CancellationToken cancellationToken = default);
    Task<Result<OccupationDto>> UpdateOccupationAsync(uint id, UpdateOccupationRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteOccupationAsync(uint id, CancellationToken cancellationToken = default);
    Task<Result<bool>> RestoreOccupationAsync(uint id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<EducationLevelDto>>> GetEducationLevelsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<Result<EducationLevelDto>> GetEducationLevelAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default);
    Task<Result<EducationLevelDto>> CreateEducationLevelAsync(CreateEducationLevelRequest request, CancellationToken cancellationToken = default);
    Task<Result<EducationLevelDto>> UpdateEducationLevelAsync(uint id, UpdateEducationLevelRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteEducationLevelAsync(uint id, CancellationToken cancellationToken = default);
    Task<Result<bool>> RestoreEducationLevelAsync(uint id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<LearningPurposeDto>>> GetLearningPurposesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<Result<LearningPurposeDto>> GetLearningPurposeAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default);
    Task<Result<LearningPurposeDto>> CreateLearningPurposeAsync(CreateLearningPurposeRequest request, CancellationToken cancellationToken = default);
    Task<Result<LearningPurposeDto>> UpdateLearningPurposeAsync(uint id, UpdateLearningPurposeRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteLearningPurposeAsync(uint id, CancellationToken cancellationToken = default);
    Task<Result<bool>> RestoreLearningPurposeAsync(uint id, CancellationToken cancellationToken = default);
}
