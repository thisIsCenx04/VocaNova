using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.Controllers;
using VocaNova.API.Features.Admin.Contracts.Responses;
using VocaNova.API.Features.Admin.BLL.Models;
using VocaNova.API.Features.Admin.DAL.Repositories;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Services;
using VocaNova.API.Features.Admin.Contracts.Requests;
using VocaNova.API.Features.Knn.Contracts.Requests;
using VocaNova.API.Features.Knn.Contracts.Responses;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.DAL.Repositories;
using VocaNova.API.Features.Knn.BLL.Services;
using VocaNova.API.Infrastructure.HostedServices;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Infrastructure.Caching.Dictionary;
using VocaNova.API.Infrastructure.Caching.Knn;
using VocaNova.API.Infrastructure.Caching.Lists;
using VocaNova.API.Infrastructure.Caching.Progress;
using VocaNova.API.Infrastructure.Caching.Quiz;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using VocaNova.Tests.Support;
using VocaNova.API.Infrastructure.RateLimiting;

namespace VocaNova.Tests.Knn;

public class AdminKnnFeatureTests
{
    [Fact]
    public async Task CreateAgeRangeAsync_Should_Save_Active_Row_And_Invalidate_Onboarding_Caches()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserLearningProfiles.AddRange(
            CreateLearningProfile(1),
            CreateLearningProfile(2));
        await dbContext.SaveChangesAsync();
        var cache = new FakeKnnTopicRecommendationCache();
        var service = CreateService(dbContext, cache);

        var result = await service.CreateAgeRangeAsync(
            new CreateAgeRangeRequest(" 18-24 ", 18, 24, 1).ToBusinessCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("18-24");
        result.Value.Status.Should().Be(UserStatus.Active);
        (await dbContext.AgeRanges.CountAsync()).Should().Be(1);
        cache.RemovedUserIds.Should().BeEquivalentTo(new[] { 1u, 2u });
    }

    [Fact]
    public async Task CreateAgeRangeAsync_Should_Reject_Duplicate_Active_Name()
    {
        await using var dbContext = CreateDbContext();
        dbContext.AgeRanges.Add(new AgeRange
        {
            AgeRangeId = 1,
            Name = "18-24",
            DisplayOrder = 1,
            Status = UserStatus.Active,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAgeRangeAsync(
            new CreateAgeRangeRequest("18-24", 18, 24, 2).ToBusinessCommand());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        result.Error.Should().Be("Age range already exists.");
    }

    [Fact]
    public async Task UpdateRegionAsync_Should_Reject_SelfParent_And_Cycle()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Regions.AddRange(
            new Region { RegionId = 1, Name = "Vietnam", Code = "VN", Status = UserStatus.Active },
            new Region { RegionId = 2, Name = "Ho Chi Minh", Code = "HCM", ParentId = 1, Status = UserStatus.Active });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var selfParent = await service.UpdateRegionAsync(
            1,
            new UpdateRegionRequest("Vietnam", "VN", 1).ToBusinessCommand());
        var cycle = await service.UpdateRegionAsync(
            1,
            new UpdateRegionRequest("Vietnam", "VN", 2).ToBusinessCommand());

        selfParent.IsSuccess.Should().BeFalse();
        selfParent.Error.Should().Be("Region cannot be its own parent.");
        cycle.IsSuccess.Should().BeFalse();
        cycle.Error.Should().Be("Parent region would create a cycle.");
    }

    [Fact]
    public async Task DeleteAndRestoreOccupationAsync_Should_Use_Soft_Delete_Status()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Occupations.Add(new Occupation
        {
            OccupationId = 1,
            Name = "Student",
            Status = UserStatus.Active,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var deleted = await service.DeleteOccupationAsync(1);
        var hidden = await service.GetOccupationAsync(1, includeDeleted: false);
        var visible = await service.GetOccupationAsync(1, includeDeleted: true);
        var restored = await service.RestoreOccupationAsync(1);

        deleted.IsSuccess.Should().BeTrue();
        hidden.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        visible.Value!.Status.Should().Be(UserStatus.Deleted);
        restored.IsSuccess.Should().BeTrue();
        (await dbContext.Occupations.SingleAsync()).Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task GetEducationLevelsAsync_Should_Filter_Search_And_IncludeDeleted()
    {
        await using var dbContext = CreateDbContext();
        dbContext.EducationLevels.AddRange(
            new EducationLevel { EducationLevelId = 1, Name = "Bachelor", DisplayOrder = 2, Status = UserStatus.Active },
            new EducationLevel { EducationLevelId = 2, Name = "Deleted Bachelor", DisplayOrder = 1, Status = UserStatus.Deleted },
            new EducationLevel { EducationLevelId = 3, Name = "High School", DisplayOrder = 3, Status = UserStatus.Active },
            new EducationLevel { EducationLevelId = 4, Name = "Locked Bachelor", DisplayOrder = 4, Status = UserStatus.Locked });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var activeOnly = await service.GetEducationLevelsAsync(new KnnLookupQuery(Q: "bachelor"));
        var includeDeleted = await service.GetEducationLevelsAsync(new KnnLookupQuery(Q: "bachelor", IncludeDeleted: true));

        activeOnly.Value!.Items.Select(item => item.EducationLevelId).Should().Equal(1u);
        includeDeleted.Value!.Items.Select(item => item.EducationLevelId).Should().Equal(2u, 1u, 4u);
    }

    [Fact]
    public void Validators_Should_Enforce_Lookup_Rules()
    {
        var ageValidator = new CreateAgeRangeRequestValidator();
        var invalidAge = ageValidator.TestValidate(new CreateAgeRangeRequest(" ", 30, 20, -1));
        invalidAge.ShouldHaveValidationErrorFor(request => request.Name);
        invalidAge.ShouldHaveValidationErrorFor(request => request.DisplayOrder);
        invalidAge.Errors.Should().Contain(error => error.ErrorMessage == "MinAge must be less than or equal to MaxAge.");

        var regionValidator = new CreateRegionRequestValidator();
        var invalidRegion = regionValidator.TestValidate(new CreateRegionRequest("Vietnam", "vietnam-code-too-long", null));
        invalidRegion.ShouldHaveValidationErrorFor(request => request.Code);

        var queryValidator = new KnnLookupRequestValidator();
        var invalidQuery = queryValidator.TestValidate(new KnnLookupRequest(Page: 0, Limit: 101, Status: "archived"));
        invalidQuery.ShouldHaveValidationErrorFor(query => query.Page);
        invalidQuery.ShouldHaveValidationErrorFor(query => query.Limit);
        invalidQuery.ShouldHaveValidationErrorFor(query => query.Status);
    }

    [Fact]
    public async Task GetConfig_Should_Return_Current_KnnOptions()
    {
        var controller = CreateController(options: CreateTunedOptions());

        var actionResult = await controller.GetConfig(CancellationToken.None);

        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<KnnConfigDto>>().Subject;
        response.Data!.Onboarding.KValue.Should().Be(7);
        response.Data.Learning.RebuildIntervalHours.Should().Be(6);
        response.Data.Learning.CacheTtlMinutes.Should().Be(90);
        response.Data.Vector.IsOverridden.Should().BeFalse();
        response.Data.Vector.Weights.Should().Be(response.Data.Vector.Defaults);
        response.Data.Vector.Storage.Should().Be("env_file");
    }

    [Fact]
    public async Task UpdateVectorWeights_Should_Write_Invariant_Env_Keys()
    {
        var store = new InMemoryRuntimeSettingsStore();
        var writer = new FakeRuntimeConfigWriter(store);
        var monitor = new MutableOptionsMonitor<KnnOptions>(CreateTunedOptions());
        var runtimeConfig = new KnnRuntimeConfigService(store, writer, monitor);
        var controller = CreateController(runtimeConfigService: runtimeConfig);
        SetAdminUser(controller, 10);

        var updated = await controller.UpdateVectorWeights(
            new UpdateKnnVectorWeightsRequest(2.5, 0.0, 1.0, 0.5, 3.0, 4.0),
            CancellationToken.None);

        var config = updated.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<ApiResponse<KnnConfigDto>>().Subject.Data!;
        config.Vector.Storage.Should().Be("env_file");

        // Written with '.' regardless of the machine's culture, otherwise the binder could not
        // read the value back.
        writer.WrittenValues["Knn__Vector__AgeRangeWeight"].Should().Be("2.5");
        writer.WrittenValues["Knn__Vector__RegionWeight"].Should().Be("0");
        writer.WrittenValues["Knn__Vector__InterestTopicsWeight"].Should().Be("4");

        // The file watcher has not fired yet, so the pipeline still sees the old configuration.
        (await runtimeConfig.GetVectorOptionsAsync()).AgeRangeWeight.Should().Be(1.0);

        // Once configuration reloads from the rewritten file, the new weights take effect.
        monitor.Set(new KnnOptions { Vector = new KnnVectorOptions { AgeRangeWeight = 2.5 } });
        (await runtimeConfig.GetVectorOptionsAsync()).AgeRangeWeight.Should().Be(2.5);
    }

    [Fact]
    public async Task UpdateVectorWeights_Should_Fall_Back_When_Env_File_Is_Not_Writable()
    {
        var store = new InMemoryRuntimeSettingsStore();
        var writer = new FakeRuntimeConfigWriter(store, canWriteEnvFile: false);
        var monitor = new MutableOptionsMonitor<KnnOptions>(CreateTunedOptions());
        var runtimeConfig = new KnnRuntimeConfigService(store, writer, monitor);
        var controller = CreateController(runtimeConfigService: runtimeConfig);
        SetAdminUser(controller, 10);

        var updated = await controller.UpdateVectorWeights(
            new UpdateKnnVectorWeightsRequest(2.5, 0.0, 1.0, 0.5, 3.0, 4.0),
            CancellationToken.None);

        var config = updated.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<ApiResponse<KnnConfigDto>>().Subject.Data!;
        config.Vector.Storage.Should().Be("fallback");
        config.Vector.CanWriteEnvFile.Should().BeFalse();

        // Nothing was written to the file, so the fallback store is what the pipeline reads —
        // and it takes effect immediately, with no watcher involved.
        writer.WrittenValues.Should().BeEmpty();
        (await runtimeConfig.GetVectorOptionsAsync()).AgeRangeWeight.Should().Be(2.5);
    }

    [Fact]
    public async Task ResetVectorWeights_Should_Write_Built_In_Defaults()
    {
        var store = new InMemoryRuntimeSettingsStore();
        var writer = new FakeRuntimeConfigWriter(store);
        var runtimeConfig = new KnnRuntimeConfigService(
            store,
            writer,
            new MutableOptionsMonitor<KnnOptions>(CreateTunedOptions()));
        var controller = CreateController(runtimeConfigService: runtimeConfig);
        SetAdminUser(controller, 10);

        await controller.UpdateVectorWeights(
            new UpdateKnnVectorWeightsRequest(2.5, 0.0, 1.0, 0.5, 3.0, 4.0),
            CancellationToken.None);
        var reset = await controller.ResetVectorWeights(CancellationToken.None);

        var config = reset.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<ApiResponse<KnnConfigDto>>().Subject.Data!;
        config.Vector.Weights.Should().BeEquivalentTo(KnnRuntimeConfigService.ToDto(new KnnVectorOptions()));

        // Defaults are written out explicitly rather than left absent, so .env stays readable.
        var defaults = new KnnVectorOptions();
        writer.WrittenValues["Knn__Vector__AgeRangeWeight"]
            .Should().Be(defaults.AgeRangeWeight.ToString(CultureInfo.InvariantCulture));
        writer.WrittenValues["Knn__Vector__InterestTopicsWeight"]
            .Should().Be(defaults.InterestTopicsWeight.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void UpdateKnnVectorWeightsRequestValidator_Should_Reject_Missing_Negative_And_AllZero()
    {
        var validator = new UpdateKnnVectorWeightsRequestValidator();

        var missing = validator.TestValidate(
            new UpdateKnnVectorWeightsRequest(1, 1, 1, 1, 1, null));
        missing.ShouldHaveValidationErrorFor(request => request.InterestTopicsWeight);

        var negative = validator.TestValidate(
            new UpdateKnnVectorWeightsRequest(-1, 1, 1, 1, 1, 1));
        negative.ShouldHaveValidationErrorFor(request => request.AgeRangeWeight);

        var tooLarge = validator.TestValidate(
            new UpdateKnnVectorWeightsRequest(1, 1, 1, 1, 1, 11));
        tooLarge.ShouldHaveValidationErrorFor(request => request.InterestTopicsWeight);

        // All-zero weights would zero every vector and kill similarity entirely.
        var allZero = validator.TestValidate(
            new UpdateKnnVectorWeightsRequest(0, 0, 0, 0, 0, 0));
        allZero.IsValid.Should().BeFalse();

        validator.TestValidate(new UpdateKnnVectorWeightsRequest(1, 0.6, 1, 0.8, 1.5, 2))
            .IsValid.Should().BeTrue();
    }

    private static KnnOptions CreateTunedOptions()
    {
        return new KnnOptions
        {
            Onboarding = new KnnOnboardingOptions
            {
                KValue = 7,
                DefaultTopicLimit = 9,
                MinSimilarity = 0.33,
                CacheTtlMinutes = 45,
            },
            Learning = new KnnLearningOptions
            {
                KValue = 11,
                MinSessions = 4,
                MinSimilarity = 0.44,
                RecommendationCount = 25,
                RebuildIntervalHours = 6,
                CacheTtlMinutes = 90,
            },
        };
    }

    [Fact]
    public void TriggerRebuild_Should_Queue_Once_Per_Admin_Per_Window()
    {
        var rebuildService = new Mock<IKnnRebuildService>();
        var controller = CreateController(
            rebuildService: rebuildService.Object,
            triggerRateLimiter: new InMemoryAdminKnnTriggerRateLimiter());
        SetAdminUser(controller, 10);

        var first = controller.TriggerRebuild();
        var second = controller.TriggerRebuild();

        first.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        second.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        rebuildService.Verify(service => service.TriggerRebuild(), Times.Once);
    }

    [Fact]
    public async Task RebuildAllAsync_Should_Continue_When_One_User_Fails()
    {
        using var serviceProvider = BuildRebuildServiceProvider(
            out var learningRepository,
            out var learningService,
            out var stateCache);
        var rebuildService = new KnnRebuildService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KnnOptions
            {
                Learning = new KnnLearningOptions { MinSessions = 2 },
            }),
            NullLogger<KnnRebuildService>.Instance,
            stateCache.Object);
        learningRepository
            .Setup(repository => repository.GetEligibleUserIdsAsync(2, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1u, 2u, 3u });
        learningService
            .Setup(service => service.GenerateWordRecommendationsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(Array.Empty<WordRecommendationItem>()));
        learningService
            .Setup(service => service.GenerateWordRecommendationsAsync(2, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("failed user"));
        learningService
            .Setup(service => service.GenerateWordRecommendationsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(Array.Empty<WordRecommendationItem>()));

        await rebuildService.RebuildAllAsync();

        learningService.Verify(service => service.GenerateWordRecommendationsAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        learningService.Verify(service => service.GenerateWordRecommendationsAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        learningService.Verify(service => service.GenerateWordRecommendationsAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        stateCache.Verify(cache => cache.SetLastRebuildAtAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AdminKnnLookupService CreateService(
        VocaNovaDbContext dbContext,
        IKnnTopicRecommendationCache? cache = null)
    {
        return new AdminKnnLookupService(
            new AdminKnnLookupRepository(dbContext),
            cache);
    }

    private static AdminKnnController CreateController(
        IAdminKnnLookupService? lookupService = null,
        IKnnRebuildService? rebuildService = null,
        IAdminKnnTriggerRateLimiter? triggerRateLimiter = null,
        KnnOptions? options = null,
        IKnnRuntimeConfigService? runtimeConfigService = null)
    {
        var effectiveOptions = options ?? new KnnOptions();

        return new AdminKnnController(
            lookupService ?? Mock.Of<IAdminKnnLookupService>(),
            rebuildService ?? Mock.Of<IKnnRebuildService>(),
            runtimeConfigService ?? CreateRuntimeConfigService(effectiveOptions),
            triggerRateLimiter ?? new InMemoryAdminKnnTriggerRateLimiter(),
            Options.Create(effectiveOptions));
    }

    private static KnnRuntimeConfigService CreateRuntimeConfigService(
        KnnOptions options,
        bool canWriteEnvFile = true)
    {
        var store = new InMemoryRuntimeSettingsStore();
        return new KnnRuntimeConfigService(
            store,
            new FakeRuntimeConfigWriter(store, canWriteEnvFile),
            new MutableOptionsMonitor<KnnOptions>(options));
    }

    private static void SetAdminUser(ControllerBase controller, uint userId)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("user_id", userId.ToString()),
                }, "test")),
            },
        };
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static ServiceProvider BuildRebuildServiceProvider(
        out Mock<IKnnLearningRepository> learningRepository,
        out Mock<IKnnLearningService> learningService,
        out Mock<IKnnRebuildStateCache> stateCache)
    {
        learningRepository = new Mock<IKnnLearningRepository>();
        learningService = new Mock<IKnnLearningService>();
        stateCache = new Mock<IKnnRebuildStateCache>();

        var services = new ServiceCollection();
        services.AddSingleton(learningRepository.Object);
        services.AddSingleton(learningService.Object);
        return services.BuildServiceProvider();
    }

    private static UserLearningProfile CreateLearningProfile(uint userId)
    {
        return new UserLearningProfile
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private sealed class FakeKnnTopicRecommendationCache : IKnnTopicRecommendationCache
    {
        public List<uint> RemovedUserIds { get; } = new();

        public Task<IReadOnlyCollection<TopicRecommendationDto>?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<TopicRecommendationDto>?>(null);
        }

        public Task SetAsync(
            uint userId,
            IReadOnlyCollection<TopicRecommendationDto> recommendations,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemovedUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }
}
