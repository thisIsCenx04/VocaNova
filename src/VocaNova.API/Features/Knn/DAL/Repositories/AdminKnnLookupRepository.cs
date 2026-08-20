using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Knn.DAL.Repositories;

public sealed class AdminKnnLookupRepository : IAdminKnnLookupRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AdminKnnLookupRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<AgeRangeLookup>> GetAgeRangesAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        ApplyCommonFilters(_dbContext.AgeRanges.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.AgeRangeId)
            .Select(item => new AgeRangeLookup(item.AgeRangeId, item.Name, item.MinAge, item.MaxAge, item.DisplayOrder, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);

    public Task<AgeRangeLookup?> FindAgeRangeAsync(uint id, CancellationToken cancellationToken = default) =>
        _dbContext.AgeRanges
            .AsNoTracking()
            .Where(item => item.AgeRangeId == id)
            .Select(item => new AgeRangeLookup(item.AgeRangeId, item.Name, item.MinAge, item.MaxAge, item.DisplayOrder, item.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> AgeRangeNameExistsAsync(
        string name,
        uint? excludingId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.AgeRanges.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.AgeRangeId != excludingId.Value),
            cancellationToken);

    public async Task<AgeRangeLookup> CreateAgeRangeAsync(
        SaveAgeRangeCommand command,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = new AgeRange
        {
            Name = command.Name!,
            MinAge = command.MinAge,
            MaxAge = command.MaxAge,
            DisplayOrder = command.DisplayOrder,
            Status = status,
        };
        _dbContext.AgeRanges.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapAgeRange(entity);
    }

    public async Task<AgeRangeLookup?> UpdateAgeRangeAsync(
        uint id,
        SaveAgeRangeCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AgeRanges.SingleOrDefaultAsync(item => item.AgeRangeId == id, cancellationToken);
        if (entity is null) return null;
        entity.Name = command.Name!;
        entity.MinAge = command.MinAge;
        entity.MaxAge = command.MaxAge;
        entity.DisplayOrder = command.DisplayOrder;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapAgeRange(entity);
    }

    public Task<bool> SetAgeRangeStatusAsync(uint id, string status, CancellationToken cancellationToken = default) =>
        SetStatusAsync(_dbContext.AgeRanges, item => item.AgeRangeId == id, status, cancellationToken);

    public Task<PagedResult<RegionLookup>> GetRegionsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default)
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
            .Select(item => new RegionLookup(item.RegionId, item.Name, item.Code, item.ParentId, item.Parent == null ? null : item.Parent.Name, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public Task<RegionLookup?> FindRegionAsync(uint id, CancellationToken cancellationToken = default) =>
        _dbContext.Regions
            .AsNoTracking()
            .Where(item => item.RegionId == id)
            .Select(item => new RegionLookup(item.RegionId, item.Name, item.Code, item.ParentId, item.Parent == null ? null : item.Parent.Name, item.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> RegionNameExistsAsync(
        string name,
        uint? excludingId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Regions.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.RegionId != excludingId.Value),
            cancellationToken);

    public Task<bool> RegionCodeExistsAsync(
        string code,
        uint? excludingId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Regions.AnyAsync(
            item => item.Code.ToLower() == code.ToLower()
                && (!excludingId.HasValue || item.RegionId != excludingId.Value),
            cancellationToken);

    public async Task<RegionLookup> CreateRegionAsync(
        string name,
        string code,
        uint? parentId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = new Region { Name = name, Code = code, ParentId = parentId, Status = status };
        _dbContext.Regions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await FindRegionAsync(entity.RegionId, cancellationToken))!;
    }

    public async Task<RegionLookup?> UpdateRegionAsync(
        uint id,
        string name,
        string code,
        uint? parentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Regions.SingleOrDefaultAsync(item => item.RegionId == id, cancellationToken);
        if (entity is null) return null;
        entity.Name = name;
        entity.Code = code;
        entity.ParentId = parentId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await FindRegionAsync(id, cancellationToken);
    }

    public Task<bool> SetRegionStatusAsync(uint id, string status, CancellationToken cancellationToken = default) =>
        SetStatusAsync(_dbContext.Regions, item => item.RegionId == id, status, cancellationToken);

    public Task<PagedResult<OccupationLookup>> GetOccupationsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        ApplyCommonFilters(_dbContext.Occupations.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.OccupationId)
            .Select(item => new OccupationLookup(item.OccupationId, item.Name, item.Description, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);

    public Task<OccupationLookup?> FindOccupationAsync(uint id, CancellationToken cancellationToken = default) =>
        _dbContext.Occupations
            .AsNoTracking()
            .Where(item => item.OccupationId == id)
            .Select(item => new OccupationLookup(item.OccupationId, item.Name, item.Description, item.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> OccupationNameExistsAsync(
        string name,
        uint? excludingId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Occupations.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.OccupationId != excludingId.Value),
            cancellationToken);

    public async Task<OccupationLookup> CreateOccupationAsync(
        string name,
        string? description,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = new Occupation { Name = name, Description = description, Status = status };
        _dbContext.Occupations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapOccupation(entity);
    }

    public async Task<OccupationLookup?> UpdateOccupationAsync(
        uint id,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Occupations.SingleOrDefaultAsync(item => item.OccupationId == id, cancellationToken);
        if (entity is null) return null;
        entity.Name = name;
        entity.Description = description;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapOccupation(entity);
    }

    public Task<bool> SetOccupationStatusAsync(uint id, string status, CancellationToken cancellationToken = default) =>
        SetStatusAsync(_dbContext.Occupations, item => item.OccupationId == id, status, cancellationToken);

    public Task<PagedResult<EducationLevelLookup>> GetEducationLevelsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        ApplyCommonFilters(_dbContext.EducationLevels.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.EducationLevelId)
            .Select(item => new EducationLevelLookup(item.EducationLevelId, item.Name, item.Description, item.DisplayOrder, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);

    public Task<EducationLevelLookup?> FindEducationLevelAsync(uint id, CancellationToken cancellationToken = default) =>
        _dbContext.EducationLevels
            .AsNoTracking()
            .Where(item => item.EducationLevelId == id)
            .Select(item => new EducationLevelLookup(item.EducationLevelId, item.Name, item.Description, item.DisplayOrder, item.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> EducationLevelNameExistsAsync(
        string name,
        uint? excludingId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.EducationLevels.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.EducationLevelId != excludingId.Value),
            cancellationToken);

    public async Task<EducationLevelLookup> CreateEducationLevelAsync(
        string name,
        string? description,
        int displayOrder,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = new EducationLevel
        {
            Name = name,
            Description = description,
            DisplayOrder = displayOrder,
            Status = status,
        };
        _dbContext.EducationLevels.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapEducationLevel(entity);
    }

    public async Task<EducationLevelLookup?> UpdateEducationLevelAsync(
        uint id,
        string name,
        string? description,
        int displayOrder,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.EducationLevels.SingleOrDefaultAsync(
            item => item.EducationLevelId == id,
            cancellationToken);
        if (entity is null) return null;
        entity.Name = name;
        entity.Description = description;
        entity.DisplayOrder = displayOrder;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapEducationLevel(entity);
    }

    public Task<bool> SetEducationLevelStatusAsync(uint id, string status, CancellationToken cancellationToken = default) =>
        SetStatusAsync(_dbContext.EducationLevels, item => item.EducationLevelId == id, status, cancellationToken);

    public Task<PagedResult<LearningPurposeLookup>> GetLearningPurposesAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        ApplyCommonFilters(_dbContext.LearningPurposes.AsNoTracking(), query, item => item.Name, item => item.Status)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.LearningPurposeId)
            .Select(item => new LearningPurposeLookup(item.LearningPurposeId, item.Name, item.Description, item.Status))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);

    public Task<LearningPurposeLookup?> FindLearningPurposeAsync(uint id, CancellationToken cancellationToken = default) =>
        _dbContext.LearningPurposes
            .AsNoTracking()
            .Where(item => item.LearningPurposeId == id)
            .Select(item => new LearningPurposeLookup(item.LearningPurposeId, item.Name, item.Description, item.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> LearningPurposeNameExistsAsync(
        string name,
        uint? excludingId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.LearningPurposes.AnyAsync(
            item => item.Status == UserStatus.Active
                && item.Name.ToLower() == name.ToLower()
                && (!excludingId.HasValue || item.LearningPurposeId != excludingId.Value),
            cancellationToken);

    public async Task<LearningPurposeLookup> CreateLearningPurposeAsync(
        string name,
        string? description,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = new LearningPurpose { Name = name, Description = description, Status = status };
        _dbContext.LearningPurposes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapLearningPurpose(entity);
    }

    public async Task<LearningPurposeLookup?> UpdateLearningPurposeAsync(
        uint id,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.LearningPurposes.SingleOrDefaultAsync(
            item => item.LearningPurposeId == id,
            cancellationToken);
        if (entity is null) return null;
        entity.Name = name;
        entity.Description = description;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapLearningPurpose(entity);
    }

    public Task<bool> SetLearningPurposeStatusAsync(uint id, string status, CancellationToken cancellationToken = default) =>
        SetStatusAsync(_dbContext.LearningPurposes, item => item.LearningPurposeId == id, status, cancellationToken);

    public async Task<IReadOnlyCollection<uint>> GetLearningProfileUserIdsAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.UserLearningProfiles
            .AsNoTracking()
            .Select(profile => profile.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    private async Task<bool> SetStatusAsync<T>(
        DbSet<T> set,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        string status,
        CancellationToken cancellationToken)
        where T : class
    {
        var entity = await set.SingleOrDefaultAsync(predicate, cancellationToken);
        if (entity is null) return false;

        typeof(T).GetProperty(nameof(AgeRange.Status))!.SetValue(entity, status);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
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

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AgeRangeLookup MapAgeRange(AgeRange entity) =>
        new(entity.AgeRangeId, entity.Name, entity.MinAge, entity.MaxAge, entity.DisplayOrder, entity.Status);

    private static RegionLookup MapRegion(Region entity) =>
        new(entity.RegionId, entity.Name, entity.Code, entity.ParentId, entity.Parent == null ? null : entity.Parent.Name,
            entity.Status);

    private static OccupationLookup MapOccupation(Occupation entity) =>
        new(entity.OccupationId, entity.Name, entity.Description, entity.Status);

    private static EducationLevelLookup MapEducationLevel(EducationLevel entity) =>
        new(entity.EducationLevelId, entity.Name, entity.Description, entity.DisplayOrder, entity.Status);

    private static LearningPurposeLookup MapLearningPurpose(LearningPurpose entity) =>
        new(entity.LearningPurposeId, entity.Name, entity.Description, entity.Status);
}
