using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Abstractions;

public interface IAdminKnnLookupRepository
{
    Task<PagedResult<AgeRangeLookup>> GetAgeRangesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<AgeRangeLookup?> FindAgeRangeAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> AgeRangeNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<AgeRangeLookup> CreateAgeRangeAsync(SaveAgeRangeCommand command, string status, CancellationToken cancellationToken = default);
    Task<AgeRangeLookup?> UpdateAgeRangeAsync(uint id, SaveAgeRangeCommand command, CancellationToken cancellationToken = default);
    Task<bool> SetAgeRangeStatusAsync(uint id, string status, CancellationToken cancellationToken = default);

    Task<PagedResult<RegionLookup>> GetRegionsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<RegionLookup?> FindRegionAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> RegionNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> RegionCodeExistsAsync(string code, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<RegionLookup> CreateRegionAsync(string name, string code, uint? parentId, string status, CancellationToken cancellationToken = default);
    Task<RegionLookup?> UpdateRegionAsync(uint id, string name, string code, uint? parentId, CancellationToken cancellationToken = default);
    Task<bool> SetRegionStatusAsync(uint id, string status, CancellationToken cancellationToken = default);

    Task<PagedResult<OccupationLookup>> GetOccupationsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<OccupationLookup?> FindOccupationAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> OccupationNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<OccupationLookup> CreateOccupationAsync(string name, string? description, string status, CancellationToken cancellationToken = default);
    Task<OccupationLookup?> UpdateOccupationAsync(uint id, string name, string? description, CancellationToken cancellationToken = default);
    Task<bool> SetOccupationStatusAsync(uint id, string status, CancellationToken cancellationToken = default);

    Task<PagedResult<EducationLevelLookup>> GetEducationLevelsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<EducationLevelLookup?> FindEducationLevelAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> EducationLevelNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<EducationLevelLookup> CreateEducationLevelAsync(string name, string? description, int displayOrder, string status, CancellationToken cancellationToken = default);
    Task<EducationLevelLookup?> UpdateEducationLevelAsync(uint id, string name, string? description, int displayOrder, CancellationToken cancellationToken = default);
    Task<bool> SetEducationLevelStatusAsync(uint id, string status, CancellationToken cancellationToken = default);

    Task<PagedResult<LearningPurposeLookup>> GetLearningPurposesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<LearningPurposeLookup?> FindLearningPurposeAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> LearningPurposeNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<LearningPurposeLookup> CreateLearningPurposeAsync(string name, string? description, string status, CancellationToken cancellationToken = default);
    Task<LearningPurposeLookup?> UpdateLearningPurposeAsync(uint id, string name, string? description, CancellationToken cancellationToken = default);
    Task<bool> SetLearningPurposeStatusAsync(uint id, string status, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetLearningProfileUserIdsAsync(CancellationToken cancellationToken = default);
}
