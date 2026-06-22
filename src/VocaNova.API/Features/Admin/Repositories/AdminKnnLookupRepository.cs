using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Admin.Repositories;

public sealed class AdminKnnLookupRepository : IAdminKnnLookupRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AdminKnnLookupRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<AgeRangeDto>> GetAgeRangesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        return ApplyCommonFilters(_dbContext.AgeRanges.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.AgeRangeId)
            .Select(item => new AgeRangeDto(item.AgeRangeId, item.Name, item.MinAge, item.MaxAge, item.DisplayOrder, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public Task<AgeRange?> FindAgeRangeAsync(uint id, CancellationToken cancellationToken = default)
    {
        return _dbContext.AgeRanges.SingleOrDefaultAsync(item => item.AgeRangeId == id, cancellationToken);
    }

    public Task<bool> AgeRangeNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.AgeRanges.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.AgeRangeId != excludingId.Value),
            cancellationToken);
    }

    public Task<PagedResult<RegionDto>> GetRegionsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedQ = NormalizeNullable(query.Q);
        var source = _dbContext.Regions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(item => item.Status == query.Status);
        }
        else if (!query.IncludeDeleted)
        {
            source = source.Where(item => item.Status == UserStatus.Active);
        }

        if (normalizedQ is not null)
        {
            var lowered = normalizedQ.ToLower();
            source = source.Where(item => item.Name.ToLower().Contains(lowered)
                || item.Code.ToLower().Contains(lowered));
        }

        return source
            .OrderBy(item => item.Name)
            .ThenBy(item => item.RegionId)
            .Select(item => new RegionDto(item.RegionId, item.Name, item.Code, item.ParentId, item.Parent == null ? null : item.Parent.Name, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public Task<Region?> FindRegionAsync(uint id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Regions.SingleOrDefaultAsync(item => item.RegionId == id, cancellationToken);
    }

    public Task<bool> RegionNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.Regions.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.RegionId != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> RegionCodeExistsAsync(string code, uint? excludingId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.Regions.AnyAsync(
            item => item.Code.ToLower() == code.ToLower()
                && (!excludingId.HasValue || item.RegionId != excludingId.Value),
            cancellationToken);
    }

    public Task<PagedResult<OccupationDto>> GetOccupationsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        return ApplyCommonFilters(_dbContext.Occupations.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.OccupationId)
            .Select(item => new OccupationDto(item.OccupationId, item.Name, item.Description, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public Task<Occupation?> FindOccupationAsync(uint id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Occupations.SingleOrDefaultAsync(item => item.OccupationId == id, cancellationToken);
    }

    public Task<bool> OccupationNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.Occupations.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.OccupationId != excludingId.Value),
            cancellationToken);
    }

    public Task<PagedResult<EducationLevelDto>> GetEducationLevelsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        return ApplyCommonFilters(_dbContext.EducationLevels.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.EducationLevelId)
            .Select(item => new EducationLevelDto(item.EducationLevelId, item.Name, item.Description, item.DisplayOrder, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public Task<EducationLevel?> FindEducationLevelAsync(uint id, CancellationToken cancellationToken = default)
    {
        return _dbContext.EducationLevels.SingleOrDefaultAsync(item => item.EducationLevelId == id, cancellationToken);
    }

    public Task<bool> EducationLevelNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.EducationLevels.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.EducationLevelId != excludingId.Value),
            cancellationToken);
    }

    public Task<PagedResult<LearningPurposeDto>> GetLearningPurposesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        return ApplyCommonFilters(_dbContext.LearningPurposes.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.LearningPurposeId)
            .Select(item => new LearningPurposeDto(item.LearningPurposeId, item.Name, item.Description, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public Task<LearningPurpose?> FindLearningPurposeAsync(uint id, CancellationToken cancellationToken = default)
    {
        return _dbContext.LearningPurposes.SingleOrDefaultAsync(item => item.LearningPurposeId == id, cancellationToken);
    }

    public Task<bool> LearningPurposeNameExistsAsync(string name, uint? excludingId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.LearningPurposes.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.LearningPurposeId != excludingId.Value),
            cancellationToken);
    }

    public void AddAgeRange(AgeRange entity) => _dbContext.AgeRanges.Add(entity);

    public void AddRegion(Region entity) => _dbContext.Regions.Add(entity);

    public void AddOccupation(Occupation entity) => _dbContext.Occupations.Add(entity);

    public void AddEducationLevel(EducationLevel entity) => _dbContext.EducationLevels.Add(entity);

    public void AddLearningPurpose(LearningPurpose entity) => _dbContext.LearningPurposes.Add(entity);

    public async Task<IReadOnlyCollection<uint>> GetLearningProfileUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Select(profile => profile.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<T> ApplyCommonFilters<T>(
        IQueryable<T> source,
        KnnLookupQuery query,
        System.Linq.Expressions.Expression<Func<T, string>> nameSelector,
        System.Linq.Expressions.Expression<Func<T, string>> statusSelector)
    {
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(BuildStatusPredicate(statusSelector, query.Status));
        }
        else if (!query.IncludeDeleted)
        {
            source = source.Where(BuildStatusPredicate(statusSelector, UserStatus.Active));
        }

        var normalizedQ = NormalizeNullable(query.Q);
        if (normalizedQ is not null)
        {
            source = source.Where(BuildSearchPredicate(nameSelector, normalizedQ));
        }

        return source;
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildStatusPredicate<T>(
        System.Linq.Expressions.Expression<Func<T, string>> statusSelector,
        string status)
    {
        var parameter = statusSelector.Parameters[0];
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
            System.Linq.Expressions.Expression.Equal(
                statusSelector.Body,
                System.Linq.Expressions.Expression.Constant(status)),
            parameter);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildSearchPredicate<T>(
        System.Linq.Expressions.Expression<Func<T, string>> nameSelector,
        string query)
    {
        var parameter = nameSelector.Parameters[0];
        var loweredName = System.Linq.Expressions.Expression.Call(
            nameSelector.Body,
            nameof(string.ToLower),
            Type.EmptyTypes);
        var contains = System.Linq.Expressions.Expression.Call(
            loweredName,
            nameof(string.Contains),
            Type.EmptyTypes,
            System.Linq.Expressions.Expression.Constant(query.ToLower()));

        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(contains, parameter);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
