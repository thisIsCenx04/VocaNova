using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Services.IServices;

namespace VocaNova.API.Features.Knn.BLL.Services;

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

    public async Task<KnnOperationResult<PagedResult<AgeRangeLookup>>> GetAgeRangesAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        KnnOperationResult<PagedResult<AgeRangeLookup>>.Success(
            await _repository.GetAgeRangesAsync(NormalizeQuery(query), cancellationToken));

    public async Task<KnnOperationResult<AgeRangeLookup>> GetAgeRangeAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindAgeRangeAsync(id, cancellationToken);
        return item is null || (!includeDeleted && item.Status == UserStatus.Deleted)
            ? KnnOperationResult<AgeRangeLookup>.NotFound("Age range not found.")
            : KnnOperationResult<AgeRangeLookup>.Success(item);
    }

    public async Task<KnnOperationResult<AgeRangeLookup>> CreateAgeRangeAsync(
        SaveAgeRangeCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = command.Name!.Trim();
        if (await _repository.AgeRangeNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return KnnOperationResult<AgeRangeLookup>.Conflict("Age range already exists.");
        }

        var saved = await _repository.CreateAgeRangeAsync(
            command with { Name = name },
            UserStatus.Active,
            cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<AgeRangeLookup>.Success(saved);
    }

    public async Task<KnnOperationResult<AgeRangeLookup>> UpdateAgeRangeAsync(
        uint id,
        SaveAgeRangeCommand command,
        CancellationToken cancellationToken = default)
    {
        var current = await _repository.FindAgeRangeAsync(id, cancellationToken);
        if (current is null || current.Status == UserStatus.Deleted)
        {
            return KnnOperationResult<AgeRangeLookup>.NotFound("Age range not found.");
        }

        var name = command.Name!.Trim();
        if (await _repository.AgeRangeNameExistsAsync(name, id, cancellationToken))
        {
            return KnnOperationResult<AgeRangeLookup>.Conflict("Age range already exists.");
        }

        var saved = await _repository.UpdateAgeRangeAsync(id, command with { Name = name }, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<AgeRangeLookup>.Success(saved!);
    }

    public Task<KnnOperationResult<bool>> DeleteAgeRangeAsync(uint id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(
            id,
            UserStatus.Deleted,
            _repository.FindAgeRangeAsync,
            _repository.SetAgeRangeStatusAsync,
            "Age range not found.",
            cancellationToken);

    public async Task<KnnOperationResult<bool>> RestoreAgeRangeAsync(
        uint id,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindAgeRangeAsync(id, cancellationToken);
        if (item is null) return KnnOperationResult<bool>.NotFound("Age range not found.");
        if (await _repository.AgeRangeNameExistsAsync(item.Name, id, cancellationToken))
        {
            return KnnOperationResult<bool>.Conflict("Age range already exists.");
        }

        await _repository.SetAgeRangeStatusAsync(id, UserStatus.Active, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<bool>.Success(true);
    }

    public async Task<KnnOperationResult<PagedResult<RegionLookup>>> GetRegionsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        KnnOperationResult<PagedResult<RegionLookup>>.Success(
            await _repository.GetRegionsAsync(NormalizeQuery(query), cancellationToken));

    public async Task<KnnOperationResult<RegionLookup>> GetRegionAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindRegionAsync(id, cancellationToken);
        return item is null || (!includeDeleted && item.Status == UserStatus.Deleted)
            ? KnnOperationResult<RegionLookup>.NotFound("Region not found.")
            : KnnOperationResult<RegionLookup>.Success(item);
    }

    public async Task<KnnOperationResult<RegionLookup>> CreateRegionAsync(
        SaveRegionCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = command.Name!.Trim();
        var code = NormalizeCode(command.Code);
        var validation = await ValidateRegionAsync(name, code, command.ParentId, null, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation.ErrorKind == KnnErrorKind.Conflict
                ? KnnOperationResult<RegionLookup>.Conflict(validation.Error!)
                : KnnOperationResult<RegionLookup>.ValidationFailure(validation.Error!);
        }

        var saved = await _repository.CreateRegionAsync(name, code, command.ParentId, UserStatus.Active, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<RegionLookup>.Success(saved);
    }

    public async Task<KnnOperationResult<RegionLookup>> UpdateRegionAsync(
        uint id,
        SaveRegionCommand command,
        CancellationToken cancellationToken = default)
    {
        var current = await _repository.FindRegionAsync(id, cancellationToken);
        if (current is null || current.Status == UserStatus.Deleted)
        {
            return KnnOperationResult<RegionLookup>.NotFound("Region not found.");
        }

        var name = command.Name!.Trim();
        var code = NormalizeCode(command.Code);
        var validation = await ValidateRegionAsync(name, code, command.ParentId, id, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation.ErrorKind == KnnErrorKind.Conflict
                ? KnnOperationResult<RegionLookup>.Conflict(validation.Error!)
                : KnnOperationResult<RegionLookup>.ValidationFailure(validation.Error!);
        }

        var saved = await _repository.UpdateRegionAsync(id, name, code, command.ParentId, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<RegionLookup>.Success(saved!);
    }

    public Task<KnnOperationResult<bool>> DeleteRegionAsync(uint id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, UserStatus.Deleted, _repository.FindRegionAsync, _repository.SetRegionStatusAsync,
            "Region not found.", cancellationToken);

    public async Task<KnnOperationResult<bool>> RestoreRegionAsync(uint id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindRegionAsync(id, cancellationToken);
        if (item is null) return KnnOperationResult<bool>.NotFound("Region not found.");
        if (await _repository.RegionNameExistsAsync(item.Name, id, cancellationToken)
            || await _repository.RegionCodeExistsAsync(item.Code, id, cancellationToken))
        {
            return KnnOperationResult<bool>.Conflict("Region already exists.");
        }

        await _repository.SetRegionStatusAsync(id, UserStatus.Active, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<bool>.Success(true);
    }

    public async Task<KnnOperationResult<PagedResult<OccupationLookup>>> GetOccupationsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        KnnOperationResult<PagedResult<OccupationLookup>>.Success(
            await _repository.GetOccupationsAsync(NormalizeQuery(query), cancellationToken));

    public async Task<KnnOperationResult<OccupationLookup>> GetOccupationAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindOccupationAsync(id, cancellationToken);
        return item is null || (!includeDeleted && item.Status == UserStatus.Deleted)
            ? KnnOperationResult<OccupationLookup>.NotFound("Occupation not found.")
            : KnnOperationResult<OccupationLookup>.Success(item);
    }

    public async Task<KnnOperationResult<OccupationLookup>> CreateOccupationAsync(
        SaveOccupationCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = command.Name!.Trim();
        if (await _repository.OccupationNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return KnnOperationResult<OccupationLookup>.Conflict("Occupation already exists.");
        }

        var saved = await _repository.CreateOccupationAsync(
            name,
            NormalizeNullable(command.Description),
            UserStatus.Active,
            cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<OccupationLookup>.Success(saved);
    }

    public async Task<KnnOperationResult<OccupationLookup>> UpdateOccupationAsync(
        uint id,
        SaveOccupationCommand command,
        CancellationToken cancellationToken = default)
    {
        var current = await _repository.FindOccupationAsync(id, cancellationToken);
        if (current is null || current.Status == UserStatus.Deleted)
        {
            return KnnOperationResult<OccupationLookup>.NotFound("Occupation not found.");
        }

        var name = command.Name!.Trim();
        if (await _repository.OccupationNameExistsAsync(name, id, cancellationToken))
        {
            return KnnOperationResult<OccupationLookup>.Conflict("Occupation already exists.");
        }

        var saved = await _repository.UpdateOccupationAsync(id, name, NormalizeNullable(command.Description), cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<OccupationLookup>.Success(saved!);
    }

    public Task<KnnOperationResult<bool>> DeleteOccupationAsync(uint id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, UserStatus.Deleted, _repository.FindOccupationAsync, _repository.SetOccupationStatusAsync,
            "Occupation not found.", cancellationToken);

    public async Task<KnnOperationResult<bool>> RestoreOccupationAsync(uint id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindOccupationAsync(id, cancellationToken);
        if (item is null) return KnnOperationResult<bool>.NotFound("Occupation not found.");
        if (await _repository.OccupationNameExistsAsync(item.Name, id, cancellationToken))
        {
            return KnnOperationResult<bool>.Conflict("Occupation already exists.");
        }

        await _repository.SetOccupationStatusAsync(id, UserStatus.Active, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<bool>.Success(true);
    }

    public async Task<KnnOperationResult<PagedResult<EducationLevelLookup>>> GetEducationLevelsAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        KnnOperationResult<PagedResult<EducationLevelLookup>>.Success(
            await _repository.GetEducationLevelsAsync(NormalizeQuery(query), cancellationToken));

    public async Task<KnnOperationResult<EducationLevelLookup>> GetEducationLevelAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindEducationLevelAsync(id, cancellationToken);
        return item is null || (!includeDeleted && item.Status == UserStatus.Deleted)
            ? KnnOperationResult<EducationLevelLookup>.NotFound("Education level not found.")
            : KnnOperationResult<EducationLevelLookup>.Success(item);
    }

    public async Task<KnnOperationResult<EducationLevelLookup>> CreateEducationLevelAsync(
        SaveEducationLevelCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = command.Name!.Trim();
        if (await _repository.EducationLevelNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return KnnOperationResult<EducationLevelLookup>.Conflict("Education level already exists.");
        }

        var saved = await _repository.CreateEducationLevelAsync(
            name,
            NormalizeNullable(command.Description),
            command.DisplayOrder,
            UserStatus.Active,
            cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<EducationLevelLookup>.Success(saved);
    }

    public async Task<KnnOperationResult<EducationLevelLookup>> UpdateEducationLevelAsync(
        uint id,
        SaveEducationLevelCommand command,
        CancellationToken cancellationToken = default)
    {
        var current = await _repository.FindEducationLevelAsync(id, cancellationToken);
        if (current is null || current.Status == UserStatus.Deleted)
        {
            return KnnOperationResult<EducationLevelLookup>.NotFound("Education level not found.");
        }

        var name = command.Name!.Trim();
        if (await _repository.EducationLevelNameExistsAsync(name, id, cancellationToken))
        {
            return KnnOperationResult<EducationLevelLookup>.Conflict("Education level already exists.");
        }

        var saved = await _repository.UpdateEducationLevelAsync(
            id,
            name,
            NormalizeNullable(command.Description),
            command.DisplayOrder,
            cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<EducationLevelLookup>.Success(saved!);
    }

    public Task<KnnOperationResult<bool>> DeleteEducationLevelAsync(uint id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, UserStatus.Deleted, _repository.FindEducationLevelAsync,
            _repository.SetEducationLevelStatusAsync, "Education level not found.", cancellationToken);

    public async Task<KnnOperationResult<bool>> RestoreEducationLevelAsync(
        uint id,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindEducationLevelAsync(id, cancellationToken);
        if (item is null) return KnnOperationResult<bool>.NotFound("Education level not found.");
        if (await _repository.EducationLevelNameExistsAsync(item.Name, id, cancellationToken))
        {
            return KnnOperationResult<bool>.Conflict("Education level already exists.");
        }

        await _repository.SetEducationLevelStatusAsync(id, UserStatus.Active, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<bool>.Success(true);
    }

    public async Task<KnnOperationResult<PagedResult<LearningPurposeLookup>>> GetLearningPurposesAsync(
        KnnLookupQuery query,
        CancellationToken cancellationToken = default) =>
        KnnOperationResult<PagedResult<LearningPurposeLookup>>.Success(
            await _repository.GetLearningPurposesAsync(NormalizeQuery(query), cancellationToken));

    public async Task<KnnOperationResult<LearningPurposeLookup>> GetLearningPurposeAsync(
        uint id,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindLearningPurposeAsync(id, cancellationToken);
        return item is null || (!includeDeleted && item.Status == UserStatus.Deleted)
            ? KnnOperationResult<LearningPurposeLookup>.NotFound("Learning purpose not found.")
            : KnnOperationResult<LearningPurposeLookup>.Success(item);
    }

    public async Task<KnnOperationResult<LearningPurposeLookup>> CreateLearningPurposeAsync(
        SaveLearningPurposeCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = command.Name!.Trim();
        if (await _repository.LearningPurposeNameExistsAsync(name, cancellationToken: cancellationToken))
        {
            return KnnOperationResult<LearningPurposeLookup>.Conflict("Learning purpose already exists.");
        }

        var saved = await _repository.CreateLearningPurposeAsync(
            name,
            NormalizeNullable(command.Description),
            UserStatus.Active,
            cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<LearningPurposeLookup>.Success(saved);
    }

    public async Task<KnnOperationResult<LearningPurposeLookup>> UpdateLearningPurposeAsync(
        uint id,
        SaveLearningPurposeCommand command,
        CancellationToken cancellationToken = default)
    {
        var current = await _repository.FindLearningPurposeAsync(id, cancellationToken);
        if (current is null || current.Status == UserStatus.Deleted)
        {
            return KnnOperationResult<LearningPurposeLookup>.NotFound("Learning purpose not found.");
        }

        var name = command.Name!.Trim();
        if (await _repository.LearningPurposeNameExistsAsync(name, id, cancellationToken))
        {
            return KnnOperationResult<LearningPurposeLookup>.Conflict("Learning purpose already exists.");
        }

        var saved = await _repository.UpdateLearningPurposeAsync(
            id,
            name,
            NormalizeNullable(command.Description),
            cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<LearningPurposeLookup>.Success(saved!);
    }

    public Task<KnnOperationResult<bool>> DeleteLearningPurposeAsync(uint id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, UserStatus.Deleted, _repository.FindLearningPurposeAsync,
            _repository.SetLearningPurposeStatusAsync, "Learning purpose not found.", cancellationToken);

    public async Task<KnnOperationResult<bool>> RestoreLearningPurposeAsync(
        uint id,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindLearningPurposeAsync(id, cancellationToken);
        if (item is null) return KnnOperationResult<bool>.NotFound("Learning purpose not found.");
        if (await _repository.LearningPurposeNameExistsAsync(item.Name, id, cancellationToken))
        {
            return KnnOperationResult<bool>.Conflict("Learning purpose already exists.");
        }

        await _repository.SetLearningPurposeStatusAsync(id, UserStatus.Active, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<bool>.Success(true);
    }

    private async Task<KnnOperationResult<bool>> SetStatusAsync<T>(
        uint id,
        string status,
        Func<uint, CancellationToken, Task<T?>> finder,
        Func<uint, string, CancellationToken, Task<bool>> setter,
        string notFoundMessage,
        CancellationToken cancellationToken)
        where T : class
    {
        var item = await finder(id, cancellationToken);
        var currentStatus = item switch
        {
            AgeRangeLookup ageRange => ageRange.Status,
            RegionLookup region => region.Status,
            OccupationLookup occupation => occupation.Status,
            EducationLevelLookup educationLevel => educationLevel.Status,
            LearningPurposeLookup learningPurpose => learningPurpose.Status,
            _ => null,
        };
        if (item is null || currentStatus == status)
        {
            return KnnOperationResult<bool>.NotFound(notFoundMessage);
        }

        await setter(id, status, cancellationToken);
        await InvalidateTopicRecommendationsAsync(cancellationToken);
        return KnnOperationResult<bool>.Success(true);
    }

    private async Task<KnnOperationResult<bool>> ValidateRegionAsync(
        string name,
        string code,
        uint? parentId,
        uint? excludingId,
        CancellationToken cancellationToken)
    {
        if (await _repository.RegionNameExistsAsync(name, excludingId, cancellationToken)
            || await _repository.RegionCodeExistsAsync(code, excludingId, cancellationToken))
        {
            return KnnOperationResult<bool>.Conflict("Region already exists.");
        }

        if (parentId is null) return KnnOperationResult<bool>.Success(true);
        if (excludingId.HasValue && parentId.Value == excludingId.Value)
        {
            return KnnOperationResult<bool>.ValidationFailure("Region cannot be its own parent.");
        }

        var parent = await _repository.FindRegionAsync(parentId.Value, cancellationToken);
        if (parent is null || parent.Status != UserStatus.Active)
        {
            return KnnOperationResult<bool>.ValidationFailure("Parent region is invalid.");
        }

        if (excludingId.HasValue && await IsDescendantRegionAsync(parentId.Value, excludingId.Value, cancellationToken))
        {
            return KnnOperationResult<bool>.ValidationFailure("Parent region would create a cycle.");
        }

        return KnnOperationResult<bool>.Success(true);
    }

    private async Task<bool> IsDescendantRegionAsync(
        uint candidateParentId,
        uint targetRegionId,
        CancellationToken cancellationToken)
    {
        var current = await _repository.FindRegionAsync(candidateParentId, cancellationToken);
        while (current?.ParentId is not null)
        {
            if (current.ParentId == targetRegionId) return true;
            current = await _repository.FindRegionAsync(current.ParentId.Value, cancellationToken);
        }

        return false;
    }

    private async Task InvalidateTopicRecommendationsAsync(CancellationToken cancellationToken)
    {
        if (_topicRecommendationCache is null) return;
        var userIds = await _repository.GetLearningProfileUserIdsAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            await _topicRecommendationCache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static KnnLookupQuery NormalizeQuery(KnnLookupQuery query) =>
        query with
        {
            Q = NormalizeNullable(query.Q),
            Status = NormalizeNullable(query.Status),
            SortBy = NormalizeNullable(query.SortBy)?.ToLowerInvariant(),
            SortDirection = NormalizeNullable(query.SortDirection)?.ToLowerInvariant(),
        };

    private static string NormalizeCode(string? value) => value!.Trim().ToUpperInvariant();

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
