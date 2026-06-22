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
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Features.Admin.Validators;
using VocaNova.API.Features.Knn;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Repositories;
using VocaNova.API.Features.Knn.Services;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
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

        var result = await service.CreateAgeRangeAsync(new CreateAgeRangeRequest(" 18-24 ", 18, 24, 1));

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

        var result = await service.CreateAgeRangeAsync(new CreateAgeRangeRequest("18-24", 18, 24, 2));

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

        var selfParent = await service.UpdateRegionAsync(1, new UpdateRegionRequest("Vietnam", "VN", 1));
        var cycle = await service.UpdateRegionAsync(1, new UpdateRegionRequest("Vietnam", "VN", 2));

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

        var queryValidator = new KnnLookupQueryValidator();
        var invalidQuery = queryValidator.TestValidate(new KnnLookupQuery(Page: 0, Limit: 101, Status: "archived"));
        invalidQuery.ShouldHaveValidationErrorFor(query => query.Page);
        invalidQuery.ShouldHaveValidationErrorFor(query => query.Limit);
        invalidQuery.ShouldHaveValidationErrorFor(query => query.Status);
    }

    [Fact]
    public void GetConfig_Should_Return_Current_KnnOptions()
    {
        var controller = CreateController(options: new KnnOptions
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
        });

        var actionResult = controller.GetConfig();

        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<KnnConfigDto>>().Subject;
        response.Data!.Onboarding.KValue.Should().Be(7);
        response.Data.Learning.RebuildIntervalHours.Should().Be(6);
        response.Data.Learning.CacheTtlMinutes.Should().Be(90);
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
            .ReturnsAsync(Result<IReadOnlyCollection<WordRecommendationItem>>.Ok(Array.Empty<WordRecommendationItem>()));
        learningService
            .Setup(service => service.GenerateWordRecommendationsAsync(2, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("failed user"));
        learningService
            .Setup(service => service.GenerateWordRecommendationsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<WordRecommendationItem>>.Ok(Array.Empty<WordRecommendationItem>()));

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
        KnnOptions? options = null)
    {
        return new AdminKnnController(
            lookupService ?? Mock.Of<IAdminKnnLookupService>(),
            rebuildService ?? Mock.Of<IKnnRebuildService>(),
            triggerRateLimiter ?? new InMemoryAdminKnnTriggerRateLimiter(),
            Options.Create(options ?? new KnnOptions()));
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
