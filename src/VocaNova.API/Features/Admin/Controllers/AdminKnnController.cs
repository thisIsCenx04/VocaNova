using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Features.Knn;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Services;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Infrastructure.Configuration;
using VocaNova.API.Infrastructure.RateLimiting;

namespace VocaNova.API.Features.Admin.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/knn")]
public sealed class AdminKnnController : ControllerBase
{
    private readonly IAdminKnnLookupService _lookupService;
    private readonly IKnnRebuildService _rebuildService;
    private readonly IKnnRuntimeConfigService _runtimeConfigService;
    private readonly IAdminKnnTriggerRateLimiter _triggerRateLimiter;
    private readonly KnnOptions _options;

    public AdminKnnController(
        IAdminKnnLookupService lookupService,
        IKnnRebuildService rebuildService,
        IKnnRuntimeConfigService runtimeConfigService,
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
        return this.OkResult(
            await MapConfigAsync(cancellationToken),
            "KNN configuration loaded successfully.");
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
            new KnnVectorWeightsDto(
                request.AgeRangeWeight!.Value,
                request.RegionWeight!.Value,
                request.OccupationWeight!.Value,
                request.EducationLevelWeight!.Value,
                request.LearningPurposeWeight!.Value,
                request.InterestTopicsWeight!.Value),
            cancellationToken);

        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "knn_vector_weights";

        return this.OkResult(
            await MapConfigAsync(cancellationToken),
            "KNN vector weights updated successfully.");
    }

    [HttpPost("config/vector-weights/reset")]
    public async Task<IActionResult> ResetVectorWeights(CancellationToken cancellationToken)
    {
        await _runtimeConfigService.ResetVectorWeightsAsync(cancellationToken);

        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "knn_vector_weights";

        return this.OkResult(
            await MapConfigAsync(cancellationToken),
            "KNN vector weights reset to deployment configuration.");
    }

    [HttpGet("rebuild-status")]
    public async Task<IActionResult> GetRebuildStatus(CancellationToken cancellationToken)
    {
        var status = await _rebuildService.GetStatusAsync(cancellationToken);
        return this.OkResult(status, "KNN rebuild status loaded successfully.");
    }

    [HttpPost("trigger-rebuild")]
    public IActionResult TriggerRebuild()
    {
        if (!TryGetCurrentUserId(out var adminUserId))
        {
            return this.ErrorResult(Result<TriggerKnnRebuildResponse>.Unauthorized("Unauthorized."));
        }

        var now = DateTime.UtcNow;
        if (!_triggerRateLimiter.IsAllowed(adminUserId, now))
        {
            return this.ErrorResult(Result<TriggerKnnRebuildResponse>.TooManyRequests(
                "KNN rebuild trigger rate limit exceeded."));
        }

        _rebuildService.TriggerRebuild();
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "knn_rebuilds";

        var response = new TriggerKnnRebuildResponse(
            "KNN rebuild has been queued.",
            now);

        return StatusCode(
            StatusCodes.Status202Accepted,
            ApiResponseFormatter.Success(response, "KNN rebuild has been queued."));
    }

    [HttpGet("age-ranges")]
    public Task<IActionResult> GetAgeRanges(
        [FromQuery] KnnLookupQuery query,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetAgeRangesAsync(query, cancellationToken),
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
            ? this.OkResult(result.Value!, "Age range loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost("age-ranges")]
    public async Task<IActionResult> CreateAgeRange(
        [FromBody] CreateAgeRangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateAgeRangeAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("age_ranges", result.Value!.AgeRangeId);
        return this.CreatedResult(result.Value, "Age range created successfully.");
    }

    [HttpPut("age-ranges/{id:uint}")]
    public async Task<IActionResult> UpdateAgeRange(
        [FromRoute] uint id,
        [FromBody] UpdateAgeRangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateAgeRangeAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("age_ranges", id);
        return this.OkResult(result.Value!, "Age range updated successfully.");
    }

    [HttpDelete("age-ranges/{id:uint}")]
    public async Task<IActionResult> DeleteAgeRange(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteAgeRangeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
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
            return this.ErrorResult(result);
        }

        SetAuditEntity("age_ranges", id);
        return this.OkResult(result.Value, "Age range restored successfully.");
    }

    [HttpGet("regions")]
    public Task<IActionResult> GetRegions(
        [FromQuery] KnnLookupQuery query,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetRegionsAsync(query, cancellationToken),
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
            ? this.OkResult(result.Value!, "Region loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost("regions")]
    public async Task<IActionResult> CreateRegion(
        [FromBody] CreateRegionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateRegionAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("regions", result.Value!.RegionId);
        return this.CreatedResult(result.Value, "Region created successfully.");
    }

    [HttpPut("regions/{id:uint}")]
    public async Task<IActionResult> UpdateRegion(
        [FromRoute] uint id,
        [FromBody] UpdateRegionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateRegionAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("regions", id);
        return this.OkResult(result.Value!, "Region updated successfully.");
    }

    [HttpDelete("regions/{id:uint}")]
    public async Task<IActionResult> DeleteRegion(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteRegionAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
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
            return this.ErrorResult(result);
        }

        SetAuditEntity("regions", id);
        return this.OkResult(result.Value, "Region restored successfully.");
    }

    [HttpGet("occupations")]
    public Task<IActionResult> GetOccupations(
        [FromQuery] KnnLookupQuery query,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetOccupationsAsync(query, cancellationToken),
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
            ? this.OkResult(result.Value!, "Occupation loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost("occupations")]
    public async Task<IActionResult> CreateOccupation(
        [FromBody] CreateOccupationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateOccupationAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("occupations", result.Value!.OccupationId);
        return this.CreatedResult(result.Value, "Occupation created successfully.");
    }

    [HttpPut("occupations/{id:uint}")]
    public async Task<IActionResult> UpdateOccupation(
        [FromRoute] uint id,
        [FromBody] UpdateOccupationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateOccupationAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("occupations", id);
        return this.OkResult(result.Value!, "Occupation updated successfully.");
    }

    [HttpDelete("occupations/{id:uint}")]
    public async Task<IActionResult> DeleteOccupation(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteOccupationAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
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
            return this.ErrorResult(result);
        }

        SetAuditEntity("occupations", id);
        return this.OkResult(result.Value, "Occupation restored successfully.");
    }

    [HttpGet("education-levels")]
    public Task<IActionResult> GetEducationLevels(
        [FromQuery] KnnLookupQuery query,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetEducationLevelsAsync(query, cancellationToken),
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
            ? this.OkResult(result.Value!, "Education level loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost("education-levels")]
    public async Task<IActionResult> CreateEducationLevel(
        [FromBody] CreateEducationLevelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateEducationLevelAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("education_levels", result.Value!.EducationLevelId);
        return this.CreatedResult(result.Value, "Education level created successfully.");
    }

    [HttpPut("education-levels/{id:uint}")]
    public async Task<IActionResult> UpdateEducationLevel(
        [FromRoute] uint id,
        [FromBody] UpdateEducationLevelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateEducationLevelAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("education_levels", id);
        return this.OkResult(result.Value!, "Education level updated successfully.");
    }

    [HttpDelete("education-levels/{id:uint}")]
    public async Task<IActionResult> DeleteEducationLevel(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteEducationLevelAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
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
            return this.ErrorResult(result);
        }

        SetAuditEntity("education_levels", id);
        return this.OkResult(result.Value, "Education level restored successfully.");
    }

    [HttpGet("learning-purposes")]
    public Task<IActionResult> GetLearningPurposes(
        [FromQuery] KnnLookupQuery query,
        CancellationToken cancellationToken)
    {
        return GetPagedAsync(
            _lookupService.GetLearningPurposesAsync(query, cancellationToken),
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
            ? this.OkResult(result.Value!, "Learning purpose loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost("learning-purposes")]
    public async Task<IActionResult> CreateLearningPurpose(
        [FromBody] CreateLearningPurposeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.CreateLearningPurposeAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("learning_purposes", result.Value!.LearningPurposeId);
        return this.CreatedResult(result.Value, "Learning purpose created successfully.");
    }

    [HttpPut("learning-purposes/{id:uint}")]
    public async Task<IActionResult> UpdateLearningPurpose(
        [FromRoute] uint id,
        [FromBody] UpdateLearningPurposeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.UpdateLearningPurposeAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity("learning_purposes", id);
        return this.OkResult(result.Value!, "Learning purpose updated successfully.");
    }

    [HttpDelete("learning-purposes/{id:uint}")]
    public async Task<IActionResult> DeleteLearningPurpose(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _lookupService.DeleteLearningPurposeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
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
            return this.ErrorResult(result);
        }

        SetAuditEntity("learning_purposes", id);
        return this.OkResult(result.Value, "Learning purpose restored successfully.");
    }

    private async Task<IActionResult> GetPagedAsync<T>(
        Task<Result<PagedResult<T>>> resultTask,
        string message)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Paged(result.Value!, message))
            : this.ErrorResult(result);
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

    private async Task<KnnConfigDto> MapConfigAsync(CancellationToken cancellationToken)
    {
        var effectiveWeights = await _runtimeConfigService.GetVectorOptionsAsync(cancellationToken);
        var isOverridden = await _runtimeConfigService.HasVectorOverrideAsync(cancellationToken);
        var storage = await _runtimeConfigService.GetStorageTargetAsync(cancellationToken);

        return new KnnConfigDto(
            new KnnOnboardingConfigDto(
                _options.Onboarding.KValue,
                _options.Onboarding.DefaultTopicLimit,
                _options.Onboarding.MinSimilarity,
                _options.Onboarding.CacheTtlMinutes),
            new KnnLearningConfigDto(
                _options.Learning.KValue,
                _options.Learning.MinSessions,
                _options.Learning.MinSimilarity,
                _options.Learning.RecommendationCount,
                _options.Learning.RebuildIntervalHours,
                _options.Learning.CacheTtlMinutes),
            new KnnVectorConfigDto(
                KnnRuntimeConfigService.ToDto(effectiveWeights),
                KnnRuntimeConfigService.ToDto(new KnnVectorOptions()),
                isOverridden,
                storage == RuntimeConfigTarget.EnvFile ? "env_file" : "fallback",
                _runtimeConfigService.CanWriteEnvFile));
    }
}
