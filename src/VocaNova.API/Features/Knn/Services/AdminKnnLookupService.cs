using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Repositories;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Knn.Services;

public sealed class AdminKnnLookupService : IAdminKnnLookupService
{
    private readonly IAdminKnnLookupRepository _repository;
    private readonly IKnnTopicRecommendationCache? _topicRecommendationCache;

    public AdminKnnLookupService(
        IAdminKnnLookupRepository repository,
        IKnnTopicRecommendationCache? topicRecommendationCache = null)
    {
        _repository = repository;
        _topicRecommendationCache = topicRecommendationCache;
    }

    public async Task<Result<PagedResult<AgeRangeDto>>> GetAgeRangesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeQuery(query);
        var result = await _repository.GetAgeRangesAsync(normalized, cancellationToken);
        return Result<PagedResult<AgeRangeDto>>.Ok(result);
    }

    public async Task<Result<AgeRangeDto>> GetAgeRangeAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindAgeRangeAsync(id, cancellationToken);
        return entity is null || (!includeDeleted && entity.Status == UserStatus.Deleted)
            ? Result<AgeRangeDto>.NotFound("Age range not found.")
            : Result<AgeRangeDto>.Ok(MapAgeRange(entity));
    }

    public async Task<Result<AgeRangeDto>> CreateAgeRangeAsync(CreateAgeRangeRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name!.Trim();
        if (await _repository.AgeRangeNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return Result<AgeRangeDto>.Conflict("Age range already exists.");
        }

        var entity = new AgeRange
        {
            Name = name,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DisplayOrder = request.DisplayOrder,
            Status = UserStatus.Active,
        };
        _repository.AddAgeRange(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<AgeRangeDto>.Ok(MapAgeRange(entity));
    }

    public async Task<Result<AgeRangeDto>> UpdateAgeRangeAsync(uint id, UpdateAgeRangeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindAgeRangeAsync(id, cancellationToken);
        if (entity is null || entity.Status == UserStatus.Deleted)
        {
            return Result<AgeRangeDto>.NotFound("Age range not found.");
        }

        var name = request.Name!.Trim();
        if (await _repository.AgeRangeNameExistsAsync(name, id, cancellationToken))
        {
            return Result<AgeRangeDto>.Conflict("Age range already exists.");
        }

        entity.Name = name;
        entity.MinAge = request.MinAge;
        entity.MaxAge = request.MaxAge;
        entity.DisplayOrder = request.DisplayOrder;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<AgeRangeDto>.Ok(MapAgeRange(entity));
    }

    public Task<Result<bool>> DeleteAgeRangeAsync(uint id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(
            id,
            UserStatus.Deleted,
            () => _repository.FindAgeRangeAsync(id, cancellationToken),
            entity => entity.Status,
            (entity, status) => entity.Status = status,
            "Age range not found.",
            cancellationToken);
    }

    public async Task<Result<bool>> RestoreAgeRangeAsync(uint id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindAgeRangeAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound("Age range not found.");
        }

        if (await _repository.AgeRangeNameExistsAsync(entity.Name, id, cancellationToken))
        {
            return Result<bool>.Conflict("Age range already exists.");
        }

        entity.Status = UserStatus.Active;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<PagedResult<RegionDto>>> GetRegionsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetRegionsAsync(NormalizeQuery(query), cancellationToken);
        return Result<PagedResult<RegionDto>>.Ok(result);
    }

    public async Task<Result<RegionDto>> GetRegionAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindRegionAsync(id, cancellationToken);
        return entity is null || (!includeDeleted && entity.Status == UserStatus.Deleted)
            ? Result<RegionDto>.NotFound("Region not found.")
            : Result<RegionDto>.Ok(MapRegion(entity));
    }

    public async Task<Result<RegionDto>> CreateRegionAsync(CreateRegionRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name!.Trim();
        var code = NormalizeCode(request.Code);
        var validation = await ValidateRegionAsync(name, code, request.ParentId, null, cancellationToken);
        if (!validation.IsSuccess)
        {
            return Result<RegionDto>.Fail(validation.Error!);
        }

        var entity = new Region
        {
            Name = name,
            Code = code,
            ParentId = request.ParentId,
            Status = UserStatus.Active,
        };
        _repository.AddRegion(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<RegionDto>.Ok(MapRegion(entity));
    }

    public async Task<Result<RegionDto>> UpdateRegionAsync(uint id, UpdateRegionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindRegionAsync(id, cancellationToken);
        if (entity is null || entity.Status == UserStatus.Deleted)
        {
            return Result<RegionDto>.NotFound("Region not found.");
        }

        var name = request.Name!.Trim();
        var code = NormalizeCode(request.Code);
        var validation = await ValidateRegionAsync(name, code, request.ParentId, id, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation.StatusCode == StatusCodes.Status409Conflict
                ? Result<RegionDto>.Conflict(validation.Error!)
                : Result<RegionDto>.Fail(validation.Error!);
        }

        entity.Name = name;
        entity.Code = code;
        entity.ParentId = request.ParentId;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<RegionDto>.Ok(MapRegion(entity));
    }

    public Task<Result<bool>> DeleteRegionAsync(uint id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(
            id,
            UserStatus.Deleted,
            () => _repository.FindRegionAsync(id, cancellationToken),
            entity => entity.Status,
            (entity, status) => entity.Status = status,
            "Region not found.",
            cancellationToken);
    }

    public async Task<Result<bool>> RestoreRegionAsync(uint id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindRegionAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound("Region not found.");
        }

        if (await _repository.RegionNameExistsAsync(entity.Name, id, cancellationToken)
            || await _repository.RegionCodeExistsAsync(entity.Code, id, cancellationToken))
        {
            return Result<bool>.Conflict("Region already exists.");
        }

        entity.Status = UserStatus.Active;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<PagedResult<OccupationDto>>> GetOccupationsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        return Result<PagedResult<OccupationDto>>.Ok(await _repository.GetOccupationsAsync(NormalizeQuery(query), cancellationToken));
    }

    public async Task<Result<OccupationDto>> GetOccupationAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindOccupationAsync(id, cancellationToken);
        return entity is null || (!includeDeleted && entity.Status == UserStatus.Deleted)
            ? Result<OccupationDto>.NotFound("Occupation not found.")
            : Result<OccupationDto>.Ok(MapOccupation(entity));
    }

    public async Task<Result<OccupationDto>> CreateOccupationAsync(CreateOccupationRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name!.Trim();
        if (await _repository.OccupationNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return Result<OccupationDto>.Conflict("Occupation already exists.");
        }

        var entity = new Occupation { Name = name, Description = NormalizeNullable(request.Description), Status = UserStatus.Active };
        _repository.AddOccupation(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<OccupationDto>.Ok(MapOccupation(entity));
    }

    public async Task<Result<OccupationDto>> UpdateOccupationAsync(uint id, UpdateOccupationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindOccupationAsync(id, cancellationToken);
        if (entity is null || entity.Status == UserStatus.Deleted)
        {
            return Result<OccupationDto>.NotFound("Occupation not found.");
        }

        var name = request.Name!.Trim();
        if (await _repository.OccupationNameExistsAsync(name, id, cancellationToken))
        {
            return Result<OccupationDto>.Conflict("Occupation already exists.");
        }

        entity.Name = name;
        entity.Description = NormalizeNullable(request.Description);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<OccupationDto>.Ok(MapOccupation(entity));
    }

    public Task<Result<bool>> DeleteOccupationAsync(uint id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(id, UserStatus.Deleted, () => _repository.FindOccupationAsync(id, cancellationToken), entity => entity.Status, (entity, status) => entity.Status = status, "Occupation not found.", cancellationToken);
    }

    public async Task<Result<bool>> RestoreOccupationAsync(uint id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindOccupationAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound("Occupation not found.");
        }

        if (await _repository.OccupationNameExistsAsync(entity.Name, id, cancellationToken))
        {
            return Result<bool>.Conflict("Occupation already exists.");
        }

        entity.Status = UserStatus.Active;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<PagedResult<EducationLevelDto>>> GetEducationLevelsAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        return Result<PagedResult<EducationLevelDto>>.Ok(await _repository.GetEducationLevelsAsync(NormalizeQuery(query), cancellationToken));
    }

    public async Task<Result<EducationLevelDto>> GetEducationLevelAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindEducationLevelAsync(id, cancellationToken);
        return entity is null || (!includeDeleted && entity.Status == UserStatus.Deleted)
            ? Result<EducationLevelDto>.NotFound("Education level not found.")
            : Result<EducationLevelDto>.Ok(MapEducationLevel(entity));
    }

    public async Task<Result<EducationLevelDto>> CreateEducationLevelAsync(CreateEducationLevelRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name!.Trim();
        if (await _repository.EducationLevelNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return Result<EducationLevelDto>.Conflict("Education level already exists.");
        }

        var entity = new EducationLevel { Name = name, Description = NormalizeNullable(request.Description), DisplayOrder = request.DisplayOrder, Status = UserStatus.Active };
        _repository.AddEducationLevel(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<EducationLevelDto>.Ok(MapEducationLevel(entity));
    }

    public async Task<Result<EducationLevelDto>> UpdateEducationLevelAsync(uint id, UpdateEducationLevelRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindEducationLevelAsync(id, cancellationToken);
        if (entity is null || entity.Status == UserStatus.Deleted)
        {
            return Result<EducationLevelDto>.NotFound("Education level not found.");
        }

        var name = request.Name!.Trim();
        if (await _repository.EducationLevelNameExistsAsync(name, id, cancellationToken))
        {
            return Result<EducationLevelDto>.Conflict("Education level already exists.");
        }

        entity.Name = name;
        entity.Description = NormalizeNullable(request.Description);
        entity.DisplayOrder = request.DisplayOrder;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<EducationLevelDto>.Ok(MapEducationLevel(entity));
    }

    public Task<Result<bool>> DeleteEducationLevelAsync(uint id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(id, UserStatus.Deleted, () => _repository.FindEducationLevelAsync(id, cancellationToken), entity => entity.Status, (entity, status) => entity.Status = status, "Education level not found.", cancellationToken);
    }

    public async Task<Result<bool>> RestoreEducationLevelAsync(uint id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindEducationLevelAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound("Education level not found.");
        }

        if (await _repository.EducationLevelNameExistsAsync(entity.Name, id, cancellationToken))
        {
            return Result<bool>.Conflict("Education level already exists.");
        }

        entity.Status = UserStatus.Active;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<PagedResult<LearningPurposeDto>>> GetLearningPurposesAsync(KnnLookupQuery query, CancellationToken cancellationToken = default)
    {
        return Result<PagedResult<LearningPurposeDto>>.Ok(await _repository.GetLearningPurposesAsync(NormalizeQuery(query), cancellationToken));
    }

    public async Task<Result<LearningPurposeDto>> GetLearningPurposeAsync(uint id, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindLearningPurposeAsync(id, cancellationToken);
        return entity is null || (!includeDeleted && entity.Status == UserStatus.Deleted)
            ? Result<LearningPurposeDto>.NotFound("Learning purpose not found.")
            : Result<LearningPurposeDto>.Ok(MapLearningPurpose(entity));
    }

    public async Task<Result<LearningPurposeDto>> CreateLearningPurposeAsync(CreateLearningPurposeRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name!.Trim();
        if (await _repository.LearningPurposeNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return Result<LearningPurposeDto>.Conflict("Learning purpose already exists.");
        }

        var entity = new LearningPurpose { Name = name, Description = NormalizeNullable(request.Description), Status = UserStatus.Active };
        _repository.AddLearningPurpose(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<LearningPurposeDto>.Ok(MapLearningPurpose(entity));
    }

    public async Task<Result<LearningPurposeDto>> UpdateLearningPurposeAsync(uint id, UpdateLearningPurposeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindLearningPurposeAsync(id, cancellationToken);
        if (entity is null || entity.Status == UserStatus.Deleted)
        {
            return Result<LearningPurposeDto>.NotFound("Learning purpose not found.");
        }

        var name = request.Name!.Trim();
        if (await _repository.LearningPurposeNameExistsAsync(name, id, cancellationToken))
        {
            return Result<LearningPurposeDto>.Conflict("Learning purpose already exists.");
        }

        entity.Name = name;
        entity.Description = NormalizeNullable(request.Description);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<LearningPurposeDto>.Ok(MapLearningPurpose(entity));
    }

    public Task<Result<bool>> DeleteLearningPurposeAsync(uint id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(id, UserStatus.Deleted, () => _repository.FindLearningPurposeAsync(id, cancellationToken), entity => entity.Status, (entity, status) => entity.Status = status, "Learning purpose not found.", cancellationToken);
    }

    public async Task<Result<bool>> RestoreLearningPurposeAsync(uint id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindLearningPurposeAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result<bool>.NotFound("Learning purpose not found.");
        }

        if (await _repository.LearningPurposeNameExistsAsync(entity.Name, id, cancellationToken))
        {
            return Result<bool>.Conflict("Learning purpose already exists.");
        }

        entity.Status = UserStatus.Active;
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    private async Task<Result<bool>> SetStatusAsync<TEntity>(
        uint id,
        string status,
        Func<Task<TEntity?>> finder,
        Func<TEntity, string> currentStatus,
        Action<TEntity, string> setStatus,
        string notFoundMessage,
        CancellationToken cancellationToken)
    {
        var entity = await finder();
        if (entity is null || currentStatus(entity) == status)
        {
            return Result<bool>.NotFound(notFoundMessage);
        }

        setStatus(entity, status);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    private async Task<Result<bool>> ValidateRegionAsync(
        string name,
        string code,
        uint? parentId,
        uint? excludingId,
        CancellationToken cancellationToken)
    {
        if (await _repository.RegionNameExistsAsync(name, excludingId, cancellationToken)
            || await _repository.RegionCodeExistsAsync(code, excludingId, cancellationToken))
        {
            return Result<bool>.Conflict("Region already exists.");
        }

        if (parentId is null)
        {
            return Result<bool>.Ok(true);
        }

        if (excludingId.HasValue && parentId.Value == excludingId.Value)
        {
            return Result<bool>.Fail("Region cannot be its own parent.");
        }

        var parent = await _repository.FindRegionAsync(parentId.Value, cancellationToken);
        if (parent is null || parent.Status != UserStatus.Active)
        {
            return Result<bool>.Fail("Parent region is invalid.");
        }

        if (excludingId.HasValue && await IsDescendantRegionAsync(parentId.Value, excludingId.Value, cancellationToken))
        {
            return Result<bool>.Fail("Parent region would create a cycle.");
        }

        return Result<bool>.Ok(true);
    }

    private async Task<bool> IsDescendantRegionAsync(
        uint candidateParentId,
        uint targetRegionId,
        CancellationToken cancellationToken)
    {
        var current = await _repository.FindRegionAsync(candidateParentId, cancellationToken);
        while (current?.ParentId is not null)
        {
            if (current.ParentId == targetRegionId)
            {
                return true;
            }

            current = await _repository.FindRegionAsync(current.ParentId.Value, cancellationToken);
        }

        return false;
    }

    private async Task InvalidateTopicRecommendationsAsync(CancellationToken cancellationToken)
    {
        if (_topicRecommendationCache is null)
        {
            return;
        }

        var userIds = await _repository.GetLearningProfileUserIdsAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            await _topicRecommendationCache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static KnnLookupQuery NormalizeQuery(KnnLookupQuery query)
    {
        return query with
        {
            Q = NormalizeNullable(query.Q),
            Status = NormalizeNullable(query.Status),
        };
    }

    private static string NormalizeCode(string? value)
    {
        return value!.Trim().ToUpperInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static AgeRangeDto MapAgeRange(AgeRange entity)
    {
        return new AgeRangeDto(entity.AgeRangeId, entity.Name, entity.MinAge, entity.MaxAge, entity.DisplayOrder, entity.Status);
    }

    private static RegionDto MapRegion(Region entity)
    {
        return new RegionDto(entity.RegionId, entity.Name, entity.Code, entity.ParentId, entity.Parent?.Name, entity.Status);
    }

    private static OccupationDto MapOccupation(Occupation entity)
    {
        return new OccupationDto(entity.OccupationId, entity.Name, entity.Description, entity.Status);
    }

    private static EducationLevelDto MapEducationLevel(EducationLevel entity)
    {
        return new EducationLevelDto(entity.EducationLevelId, entity.Name, entity.Description, entity.DisplayOrder, entity.Status);
    }

    private static LearningPurposeDto MapLearningPurpose(LearningPurpose entity)
    {
        return new LearningPurposeDto(entity.LearningPurposeId, entity.Name, entity.Description, entity.Status);
    }
}
