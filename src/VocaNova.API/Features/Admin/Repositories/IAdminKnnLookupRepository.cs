using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Admin.Repositories;

public interface IAdminKnnLookupRepository
{
    Task<PagedResult<AgeRangeDto>> GetAgeRangesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<AgeRange?> FindAgeRangeAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> AgeRangeNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<RegionDto>> GetRegionsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<Region?> FindRegionAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> RegionNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> RegionCodeExistsAsync(string code, uint? excludingId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<OccupationDto>> GetOccupationsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<Occupation?> FindOccupationAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> OccupationNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<EducationLevelDto>> GetEducationLevelsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<EducationLevel?> FindEducationLevelAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> EducationLevelNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<LearningPurposeDto>> GetLearningPurposesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default);
    Task<LearningPurpose?> FindLearningPurposeAsync(uint id, CancellationToken cancellationToken = default);
    Task<bool> LearningPurposeNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default);

    void AddAgeRange(AgeRange entity);
    void AddRegion(Region entity);
    void AddOccupation(Occupation entity);
    void AddEducationLevel(EducationLevel entity);
    void AddLearningPurpose(LearningPurpose entity);

    Task<IReadOnlyCollection<uint>> GetLearningProfileUserIdsAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
