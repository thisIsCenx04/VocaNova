using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Abstractions.Configuration;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Services;
using VocaNova.API.Features.Knn.Contracts.Requests;
using VocaNova.API.Features.Knn.Mappings;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;

namespace VocaNova.API.Features.Knn.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/knn")]
public sealed class AdminKnnController : ControllerBase
{
    private readonly IAdminKnnLookupService _lookupService;
    private readonly IKnnRebuildService _rebuildService;
    private readonly IKnnRuntimeConfigurationService _runtimeConfigService;
    private readonly IAdminKnnTriggerRateLimiter _triggerRateLimiter;
    private readonly KnnOptions _options;

    public AdminKnnController(
        IAdminKnnLookupService lookupService,
        IKnnRebuildService rebuildService,
        IKnnRuntimeConfigurationService runtimeConfigService,
        IAdminKnnTriggerRateLimiter triggerRateLimiter,
        IOptions<KnnOptions> options)
    {
        _lookupService = lookupService;
        _rebuildService = rebuildService;
        _runtimeConfigService = runtimeConfigService;
        _triggerRateLimiter = triggerRateLimiter;
        _options = options.Value;
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        return Ok(ApiResponseFormatter.Success(
            (await MapConfigAsync(cancellationToken)).ToResponse(),
            "KNN configuration loaded successfully."));
    }

    /// <summary>
    /// Retunes the profile-vector weights without a redeploy. New weights apply to the next
    /// recommendation that is computed; per-user recommendations already cached keep serving
    /// until their TTL expires (see <see cref="KnnOnboardingOptions.CacheTtlMinutes"/>), so a
    /// change is not visible to every user instantly.
    /// </summary>
    [HttpPut("config/vector-weights")]
    public async Task<IActionResult> UpdateVectorWeights(
        [FromBody] UpdateKnnVectorWeightsRequest request,
        CancellationToken cancellationToken)
    {
        await _runtimeConfigService.UpdateVectorWeightsAsync(
            new KnnVectorWeights(
                request.AgeRangeWeight!.Value,
                request.RegionWeight!.Value,
                request.OccupationWeight!.Value,
                request.EducationLevelWeight!.Value,
                request.LearningPurposeWeight!.Value,
                request.InterestTopicsWeight!.Value),
            cancellationToken);

        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "knn_vector_weights";

        return Ok(ApiResponseFormatter.Success(
            (await MapConfigAsync(cancellationToken)).ToResponse(),
            "KNN vector weights updated successfully."));
    }

    [HttpPost("config/vector-weights/reset")]
    public async Task<IActionResult> ResetVectorWeights(CancellationToken cancellationToken)
    {
        await _runtimeConfigService.ResetVectorWeightsAsync(cancellationToken);

        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "knn_vector_weights";

        return Ok(ApiResponseFormatter.Success(
            (await MapConfigAsync(cancellationToken)).ToResponse(),
            "KNN vector weights reset to deployment configuration."));
    }

    [HttpGet("rebuild-status")]
    public async Task<IActionResult> GetRebuildStatus(CancellationToken cancellationToken)
    {
        var status = await _rebuildService.GetStatusAsync(cancellationToken);
        return Ok(ApiResponseFormatter.Success(status.ToResponse(), "KNN rebuild status loaded successfully."));
    }

    [HttpPost("trigger-rebuild")]
    public IActionResult TriggerRebuild()
    {
        if (!TryGetCurrentUserId(out var adminUserId))
        {
            return ErrorResponse(KnnOperationResult<TriggerKnnRebuildResult>.Unauthorized("Unauthorized."));
        }

        var now = DateTime.UtcNow;
        if (!_triggerRateLimiter.IsAllowed(adminUserId, now))
        {
            return ErrorResponse(KnnOperationResult<TriggerKnnRebuildResult>.TooManyRequests(
                "KNN rebuild trigger rate limit exceeded."));
        }

        _rebuildService.TriggerRebuild();
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "knn_rebuilds";

        var response = new TriggerKnnRebuildResult(
            "KNN rebuild has been queued.",
            now);

        return StatusCode(
            StatusCodes.Status202Accepted,
            ApiResponseFormatter.Success(response.ToResponse(), "KNN rebuild has been queued."));
    }

    [HttpGet("age-ranges")]
    public Task<IActionResult> GetAgeRanges(
        [FromQuery] KnnLookupRequest request,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetAgeRangesAsync(request.ToBusinessQuery(), cancellationToken), value => value.ToResponse(),
            "Age ranges loaded successfully.");
    }

    [HttpGet("age-ranges/{id:uint}")]
    public async Task<IActionResult> GetAgeRange(
        [FromRoute] uint id,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetAgeRangeAsync(id, includeDeleted, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Age range loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("age-ranges")]
    public async Task<IActionResult> CreateAgeRange(
        [FromBody] CreateAgeRangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateAgeRangeAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("age_ranges", result.Value!.AgeRangeId);
        return StatusCode(StatusCodes.Status201Created, ApiResponseFormatter.Created(result.Value!.ToResponse(), "Age range created successfully."));
    }

    [HttpPut("age-ranges/{id:uint}")]
    public async Task<IActionResult> UpdateAgeRange(
        [FromRoute] uint id,
        [FromBody] UpdateAgeRangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateAgeRangeAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("age_ranges", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Age range updated successfully."));
    }

    [HttpDelete("age-ranges/{id:uint}")]
    public async Task<IActionResult> DeleteAgeRange(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteAgeRangeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("age_ranges", id);
        return this.OkResult(result.Value, "Age range deleted successfully.");
    }

    [HttpPatch("age-ranges/{id:uint}/restore")]
    public async Task<IActionResult> RestoreAgeRange(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.RestoreAgeRangeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("age_ranges", id);
        return this.OkResult(result.Value, "Age range restored successfully.");
    }

    [HttpGet("regions")]
    public Task<IActionResult> GetRegions(
        [FromQuery] KnnLookupRequest request,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetRegionsAsync(request.ToBusinessQuery(), cancellationToken), value => value.ToResponse(),
            "Regions loaded successfully.");
    }

    [HttpGet("regions/{id:uint}")]
    public async Task<IActionResult> GetRegion(
        [FromRoute] uint id,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetRegionAsync(id, includeDeleted, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Region loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("regions")]
    public async Task<IActionResult> CreateRegion(
        [FromBody] CreateRegionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateRegionAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("regions", result.Value!.RegionId);
        return StatusCode(StatusCodes.Status201Created, ApiResponseFormatter.Created(result.Value!.ToResponse(), "Region created successfully."));
    }

    [HttpPut("regions/{id:uint}")]
    public async Task<IActionResult> UpdateRegion(
        [FromRoute] uint id,
        [FromBody] UpdateRegionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateRegionAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("regions", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Region updated successfully."));
    }

    [HttpDelete("regions/{id:uint}")]
    public async Task<IActionResult> DeleteRegion(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteRegionAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("regions", id);
        return this.OkResult(result.Value, "Region deleted successfully.");
    }

    [HttpPatch("regions/{id:uint}/restore")]
    public async Task<IActionResult> RestoreRegion(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.RestoreRegionAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("regions", id);
        return this.OkResult(result.Value, "Region restored successfully.");
    }

    [HttpGet("occupations")]
    public Task<IActionResult> GetOccupations(
        [FromQuery] KnnLookupRequest request,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetOccupationsAsync(request.ToBusinessQuery(), cancellationToken), value => value.ToResponse(),
            "Occupations loaded successfully.");
    }

    [HttpGet("occupations/{id:uint}")]
    public async Task<IActionResult> GetOccupation(
        [FromRoute] uint id,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetOccupationAsync(id, includeDeleted, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Occupation loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("occupations")]
    public async Task<IActionResult> CreateOccupation(
        [FromBody] CreateOccupationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateOccupationAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("occupations", result.Value!.OccupationId);
        return StatusCode(StatusCodes.Status201Created, ApiResponseFormatter.Created(result.Value!.ToResponse(), "Occupation created successfully."));
    }

    [HttpPut("occupations/{id:uint}")]
    public async Task<IActionResult> UpdateOccupation(
        [FromRoute] uint id,
        [FromBody] UpdateOccupationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateOccupationAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("occupations", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Occupation updated successfully."));
    }

    [HttpDelete("occupations/{id:uint}")]
    public async Task<IActionResult> DeleteOccupation(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteOccupationAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("occupations", id);
        return this.OkResult(result.Value, "Occupation deleted successfully.");
    }

    [HttpPatch("occupations/{id:uint}/restore")]
    public async Task<IActionResult> RestoreOccupation(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.RestoreOccupationAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("occupations", id);
        return this.OkResult(result.Value, "Occupation restored successfully.");
    }

    [HttpGet("education-levels")]
    public Task<IActionResult> GetEducationLevels(
        [FromQuery] KnnLookupRequest request,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetEducationLevelsAsync(request.ToBusinessQuery(), cancellationToken), value => value.ToResponse(),
            "Education levels loaded successfully.");
    }

    [HttpGet("education-levels/{id:uint}")]
    public async Task<IActionResult> GetEducationLevel(
        [FromRoute] uint id,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetEducationLevelAsync(id, includeDeleted, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Education level loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("education-levels")]
    public async Task<IActionResult> CreateEducationLevel(
        [FromBody] CreateEducationLevelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateEducationLevelAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("education_levels", result.Value!.EducationLevelId);
        return StatusCode(StatusCodes.Status201Created, ApiResponseFormatter.Created(result.Value!.ToResponse(), "Education level created successfully."));
    }

    [HttpPut("education-levels/{id:uint}")]
    public async Task<IActionResult> UpdateEducationLevel(
        [FromRoute] uint id,
        [FromBody] UpdateEducationLevelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateEducationLevelAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("education_levels", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Education level updated successfully."));
    }

    [HttpDelete("education-levels/{id:uint}")]
    public async Task<IActionResult> DeleteEducationLevel(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteEducationLevelAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("education_levels", id);
        return this.OkResult(result.Value, "Education level deleted successfully.");
    }

    [HttpPatch("education-levels/{id:uint}/restore")]
    public async Task<IActionResult> RestoreEducationLevel(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.RestoreEducationLevelAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("education_levels", id);
        return this.OkResult(result.Value, "Education level restored successfully.");
    }

    [HttpGet("learning-purposes")]
    public Task<IActionResult> GetLearningPurposes(
        [FromQuery] KnnLookupRequest request,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetLearningPurposesAsync(request.ToBusinessQuery(), cancellationToken), value => value.ToResponse(),
            "Learning purposes loaded successfully.");
    }

    [HttpGet("learning-purposes/{id:uint}")]
    public async Task<IActionResult> GetLearningPurpose(
        [FromRoute] uint id,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetLearningPurposeAsync(id, includeDeleted, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Learning purpose loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("learning-purposes")]
    public async Task<IActionResult> CreateLearningPurpose(
        [FromBody] CreateLearningPurposeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateLearningPurposeAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("learning_purposes", result.Value!.LearningPurposeId);
        return StatusCode(StatusCodes.Status201Created, ApiResponseFormatter.Created(result.Value!.ToResponse(), "Learning purpose created successfully."));
    }

    [HttpPut("learning-purposes/{id:uint}")]
    public async Task<IActionResult> UpdateLearningPurpose(
        [FromRoute] uint id,
        [FromBody] UpdateLearningPurposeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateLearningPurposeAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("learning_purposes", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Learning purpose updated successfully."));
    }

    [HttpDelete("learning-purposes/{id:uint}")]
    public async Task<IActionResult> DeleteLearningPurpose(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteLearningPurposeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("learning_purposes", id);
        return this.OkResult(result.Value, "Learning purpose deleted successfully.");
    }

    [HttpPatch("learning-purposes/{id:uint}/restore")]
    public async Task<IActionResult> RestoreLearningPurpose(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.RestoreLearningPurposeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return ErrorResponse(result);
        }

        SetAuditEntity("learning_purposes", id);
        return this.OkResult(result.Value, "Learning purpose restored successfully.");
    }

    private async Task<IActionResult> GetPagedAsync<TBusiness, TResponse>(
        Task<KnnOperationResult<PagedResult<TBusiness>>> resultTask,
        Func<PagedResult<TBusiness>, PagedResult<TResponse>> map,
        string message)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Paged(map(result.Value!), message))
            : ErrorResponse(result);
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }

    private void SetAuditEntity(string entityType, uint entityId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = entityType;
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = entityId;
    }

    private ObjectResult ErrorResponse<T>(KnnOperationResult<T> result)
    {
        var status = result.ErrorKind switch
        {
            KnnErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            KnnErrorKind.NotFound => StatusCodes.Status404NotFound,
            KnnErrorKind.Conflict => StatusCodes.Status409Conflict,
            KnnErrorKind.TooManyRequests => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, ApiResponseFormatter.Error(result.Error!, [result.Error!]));
    }

    private async Task<KnnConfig> MapConfigAsync(CancellationToken cancellationToken)
    {
        var effectiveWeights = await _runtimeConfigService.GetVectorOptionsAsync(cancellationToken);
        var isOverridden = await _runtimeConfigService.HasVectorOverrideAsync(cancellationToken);
        var storage = await _runtimeConfigService.GetStorageTargetAsync(cancellationToken);

        return new KnnConfig(
            new KnnOnboardingConfig(
                _options.Onboarding.KValue,
                _options.Onboarding.DefaultTopicLimit,
                _options.Onboarding.MinSimilarity,
                _options.Onboarding.CacheTtlMinutes),
            new KnnLearningConfig(
                _options.Learning.KValue,
                _options.Learning.MinSessions,
                _options.Learning.MinSimilarity,
                _options.Learning.RecommendationCount,
                _options.Learning.RebuildIntervalHours,
                _options.Learning.CacheTtlMinutes),
            new KnnVectorConfig(
                KnnRuntimeConfigurationService.ToDto(effectiveWeights),
                KnnRuntimeConfigurationService.ToDto(new KnnVectorOptions()),
                isOverridden,
                storage == RuntimeConfigTarget.EnvFile ? "env_file" : "fallback",
                _runtimeConfigService.CanWriteEnvFile));
    }
}
