using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Repositories;
using VocaNova.API.Features.Knn.Services;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using VocaNova.Tests.Support;

namespace VocaNova.Tests.Knn;

public class KnnOnboardingRecommendationTests
{
    [Fact]
    public void CosineSimilarity_Should_Handle_Identical_Orthogonal_And_Zero_Vectors()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        service.CosineSimilarity(new[] { 1.0, 0.0, 1.0 }, new[] { 1.0, 0.0, 1.0 })
            .Should()
            .BeApproximately(1.0, 0.0001);
        service.CosineSimilarity(new[] { 1.0, 0.0 }, new[] { 0.0, 1.0 })
            .Should()
            .Be(0.0);
        service.CosineSimilarity(new[] { 0.0, 0.0 }, new[] { 1.0, 0.0 })
            .Should()
            .Be(0.0);
    }

    [Fact]
    public async Task ComputeProfileVectorAsync_Should_Weight_Only_Active_Lookups()
    {
        await using var dbContext = CreateDbContext();
        await SeedLookupsAsync(dbContext);
        dbContext.Users.Add(CreateUser(1));
        dbContext.UserLearningProfiles.Add(new UserLearningProfile
        {
            UserId = 1,
            AgeRangeId = 1,
            RegionId = null,
            OccupationId = 1,
            EducationLevelId = 2,
            LearningPurposeId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var vector = await service.ComputeProfileVectorAsync(1);

        // Blocks in order: age (2 active), region (1), occupation (1), education (1 active;
        // the selected level is soft-deleted so it contributes nothing), purpose (1).
        // Each hit carries its configured weight rather than a bare 1.0.
        var weights = CreateOptions().Vector;
        vector.Should().Equal(
            weights.AgeRangeWeight,
            0.0,
            0.0,
            weights.OccupationWeight,
            0.0,
            weights.LearningPurposeWeight);
    }

    [Fact]
    public async Task ComputeProfileVectorAsync_Should_Use_Admin_Tuned_Weights_Over_Configuration()
    {
        await using var dbContext = CreateDbContext();
        await SeedLookupsAsync(dbContext);
        dbContext.Users.Add(CreateUser(1));
        dbContext.UserLearningProfiles.Add(new UserLearningProfile
        {
            UserId = 1,
            AgeRangeId = 1,
            LearningPurposeId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var options = CreateOptions();
        var monitor = new MutableOptionsMonitor<KnnOptions>(options);
        var store = new InMemoryRuntimeSettingsStore();
        var runtimeConfig = new KnnRuntimeConfigService(
            store,
            new FakeRuntimeConfigWriter(store),
            monitor);
        var service = CreateService(dbContext, runtimeConfig: runtimeConfig);

        var beforeReload = await service.ComputeProfileVectorAsync(1);
        beforeReload[0].Should().Be(options.Vector.AgeRangeWeight);

        // Saving rewrites .env; the value reaches the pipeline once configuration reloads.
        await runtimeConfig.UpdateVectorWeightsAsync(
            new KnnVectorWeightsDto(3.0, 0.6, 1.0, 0.8, 1.5, 2.0));
        monitor.Set(new KnnOptions
        {
            Onboarding = options.Onboarding,
            Vector = new KnnVectorOptions { AgeRangeWeight = 3.0 },
        });

        var afterReload = await service.ComputeProfileVectorAsync(1);
        afterReload[0].Should().Be(3.0);
    }

    [Fact]
    public async Task ComputeProfileVectorAsync_Should_Use_Fallback_Weights_When_Env_File_Is_Not_Writable()
    {
        await using var dbContext = CreateDbContext();
        await SeedLookupsAsync(dbContext);
        dbContext.Users.Add(CreateUser(1));
        dbContext.UserLearningProfiles.Add(new UserLearningProfile
        {
            UserId = 1,
            AgeRangeId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var options = CreateOptions();
        var store = new InMemoryRuntimeSettingsStore();
        var runtimeConfig = new KnnRuntimeConfigService(
            store,
            new FakeRuntimeConfigWriter(store, canWriteEnvFile: false),
            new MutableOptionsMonitor<KnnOptions>(options));
        var service = CreateService(dbContext, runtimeConfig: runtimeConfig);

        await runtimeConfig.UpdateVectorWeightsAsync(
            new KnnVectorWeightsDto(3.0, 0.6, 1.0, 0.8, 1.5, 2.0));

        // No file to watch, so the fallback store applies straight away.
        var vector = await service.ComputeProfileVectorAsync(1);
        vector[0].Should().Be(3.0);
    }

    [Fact]
    public async Task ComputeProfileVectorAsync_Should_Normalize_Interest_Topic_Block()
    {
        await using var dbContext = CreateDbContext();
        await SeedLookupsAsync(dbContext);
        dbContext.Users.Add(CreateUser(1));
        dbContext.Topics.AddRange(CreateTopic(101, "Travel"), CreateTopic(102, "Business"));
        dbContext.UserTopicPreferences.AddRange(
            CreatePreference(1, 101, TopicPreferenceSource.Onboarding),
            CreatePreference(1, 102, TopicPreferenceSource.Onboarding));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var vector = await service.ComputeProfileVectorAsync(1);

        // Two picks share the block's unit length, so the block never grows just because the
        // user selected more topics.
        var weights = CreateOptions().Vector;
        var expectedTopicValue = weights.InterestTopicsWeight / Math.Sqrt(2);
        // 6 lookup dimensions (2 age + 1 region + 1 occupation + 1 education + 1 purpose)
        // followed by the 2 topic dimensions.
        vector.Should().HaveCount(8);
        vector[6].Should().BeApproximately(expectedTopicValue, 0.0001);
        vector[7].Should().BeApproximately(expectedTopicValue, 0.0001);
        var topicBlockLength = Math.Sqrt((vector[6] * vector[6]) + (vector[7] * vector[7]));
        topicBlockLength.Should().BeApproximately(weights.InterestTopicsWeight, 0.0001);
    }

    [Fact]
    public async Task ComputeProfileVectorAsync_Should_Use_Topic_Picks_When_Profile_Row_Is_Missing()
    {
        await using var dbContext = CreateDbContext();
        await SeedLookupsAsync(dbContext);
        dbContext.Users.Add(CreateUser(1));
        dbContext.Topics.Add(CreateTopic(101, "Travel"));
        dbContext.UserTopicPreferences.Add(
            CreatePreference(1, 101, TopicPreferenceSource.Onboarding));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var vector = await service.ComputeProfileVectorAsync(1);

        vector.Should().NotBeEmpty();
        vector.Last().Should().BeApproximately(CreateOptions().Vector.InterestTopicsWeight, 0.0001);
    }

    [Fact]
    public async Task RecommendTopicsAsync_Should_Return_Empty_When_No_Data_Exists()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.RecommendTopicsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task RecommendTopicsAsync_Should_Fallback_To_Popular_Topics_When_User_Has_No_Profile()
    {
        await using var dbContext = CreateDbContext();
        await SeedFallbackDataAsync(dbContext);
        dbContext.UserLearningProfiles.RemoveRange(dbContext.UserLearningProfiles);
        dbContext.UserTopicPreferences.RemoveRange(
            dbContext.UserTopicPreferences.Where(preference => preference.UserId == 1));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.RecommendTopicsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(topic => topic.TopicId).Should().Equal(101u, 102u);
    }

    [Fact]
    public async Task RecommendTopicsAsync_Should_Use_Nearest_Neighbors_And_Exclude_Current_User_Topics()
    {
        await using var dbContext = CreateDbContext();
        await SeedRecommendationDataAsync(dbContext);
        var cache = new FakeKnnTopicRecommendationCache();
        var service = CreateService(dbContext, cache);

        var result = await service.RecommendTopicsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(topic => topic.TopicId).Should().Equal(101u);
        cache.SetCount.Should().Be(1);

        // Only user 2 clears the similarity threshold, so the topic score is exactly that
        // neighbour's similarity. It sits below 1.0 because the two users' topic picks differ
        // even though every demographic answer matches.
        var expectedSimilarity = service.CosineSimilarity(
            await service.ComputeProfileVectorAsync(1),
            await service.ComputeProfileVectorAsync(2));
        expectedSimilarity.Should().BeLessThan(1.0);
        result.Value!.Single().RecommendationScore.Should().BeApproximately(expectedSimilarity, 0.0001);
    }

    [Fact]
    public async Task SelectTopicsAsync_Should_Store_Onboarding_Picks_And_Retire_Deselected_Ones()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Topics.AddRange(
            CreateTopic(101, "Travel"),
            CreateTopic(102, "Business"),
            CreateTopic(103, "Cooking"));
        dbContext.UserTopicPreferences.AddRange(
            CreatePreference(1, 101, TopicPreferenceSource.Onboarding),
            CreatePreference(1, 103, TopicPreferenceSource.KnnSuggested));
        await dbContext.SaveChangesAsync();
        var cache = new FakeKnnTopicRecommendationCache();
        var service = CreateService(dbContext, cache);

        var result = await service.SelectTopicsAsync(1, new uint[] { 102 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        var preferences = await dbContext.UserTopicPreferences
            .OrderBy(preference => preference.TopicId)
            .ToListAsync();
        preferences.Single(preference => preference.TopicId == 101).Status
            .Should().Be(UserStatus.Deleted);
        preferences.Single(preference => preference.TopicId == 102).Source
            .Should().Be(TopicPreferenceSource.Onboarding);
        preferences.Single(preference => preference.TopicId == 102).Status
            .Should().Be(UserStatus.Active);
        // A topic the user accepted from a KNN suggestion is not an onboarding pick, so
        // deselecting during onboarding must leave it untouched.
        preferences.Single(preference => preference.TopicId == 103).Status
            .Should().Be(UserStatus.Active);
        cache.RemoveCount.Should().Be(1);
    }

    [Fact]
    public async Task SelectTopicsAsync_Should_Return_NotFound_For_Unknown_Topic()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.SelectTopicsAsync(1, new uint[] { 999 });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetLearningProfileOptionsAsync_Should_Expose_Only_Active_Lookups()
    {
        await using var dbContext = CreateDbContext();
        await SeedLookupsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetLearningProfileOptionsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.AgeRanges.Select(option => option.Id).Should().Equal(1u, 2u);
        result.Value!.EducationLevels.Select(option => option.Id).Should().Equal(1u);
        result.Value!.LearningPurposes.Select(option => option.Name).Should().Equal("Work");
    }

    [Fact]
    public async Task RecommendTopicsAsync_Should_Fallback_When_Profile_Vector_Is_All_Zero()
    {
        await using var dbContext = CreateDbContext();
        await SeedFallbackDataAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.RecommendTopicsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(topic => topic.TopicId).Should().Equal(101u, 102u);
        result.Value!.First().RecommendationScore.Should().Be(2.0);
    }

    [Fact]
    public async Task RecommendTopicsAsync_Should_Return_Cached_Recommendations_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cached = new[]
        {
            new TopicRecommendationDto(1, "A", null, null, 0, 0.9),
            new TopicRecommendationDto(2, "B", null, null, 0, 0.8),
        };
        var cache = new FakeKnnTopicRecommendationCache(cached);
        var service = CreateService(dbContext, cache);

        var result = await service.RecommendTopicsAsync(1, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value!.Single().TopicId.Should().Be(1);
        cache.GetCount.Should().Be(1);
        cache.SetCount.Should().Be(0);
    }

    [Fact]
    public async Task AcceptTopicAsync_Should_Upsert_KnnSuggested_Source_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTime.UtcNow;
        dbContext.Topics.Add(new Topic
        {
            TopicId = 101,
            TopicName = "Travel",
            Status = UserStatus.Active,
        });
        dbContext.UserTopicPreferences.Add(new UserTopicPreference
        {
            UserId = 1,
            TopicId = 101,
            Source = TopicPreferenceSource.Onboarding,
            Status = UserStatus.Deleted,
            CreatedAt = now,
        });
        await dbContext.SaveChangesAsync();
        var cache = new FakeKnnTopicRecommendationCache();
        var service = CreateService(dbContext, cache);

        var result = await service.AcceptTopicAsync(1, 101);

        result.IsSuccess.Should().BeTrue();
        var preference = await dbContext.UserTopicPreferences.SingleAsync();
        preference.Source.Should().Be(TopicPreferenceSource.KnnSuggested);
        preference.Status.Should().Be(UserStatus.Active);
        cache.RemoveCount.Should().Be(1);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static KnnOnboardingService CreateService(
        VocaNovaDbContext dbContext,
        IKnnTopicRecommendationCache? cache = null,
        KnnOptions? options = null,
        IKnnRuntimeConfigService? runtimeConfig = null)
    {
        return new KnnOnboardingService(
            new KnnProfileRepository(dbContext),
            Options.Create(options ?? CreateOptions()),
            cache,
            runtimeConfig);
    }

    private static KnnOptions CreateOptions()
    {
        return new KnnOptions
        {
            Onboarding = new KnnOnboardingOptions
            {
                KValue = 5,
                DefaultTopicLimit = 10,
                MinSimilarity = 0.1,
                CacheTtlMinutes = 30,
            },
            Vector = new KnnVectorOptions(),
        };
    }

    private static async Task SeedLookupsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.AgeRanges.AddRange(
            new AgeRange { AgeRangeId = 1, Name = "18-24", DisplayOrder = 1, Status = UserStatus.Active },
            new AgeRange { AgeRangeId = 2, Name = "25-34", DisplayOrder = 2, Status = UserStatus.Active });
        dbContext.Regions.Add(new Region { RegionId = 1, Name = "Ho Chi Minh", Code = "HCM", Status = UserStatus.Active });
        dbContext.Occupations.Add(new Occupation { OccupationId = 1, Name = "Student", Status = UserStatus.Active });
        dbContext.EducationLevels.AddRange(
            new EducationLevel { EducationLevelId = 1, Name = "High School", DisplayOrder = 1, Status = UserStatus.Active },
            new EducationLevel { EducationLevelId = 2, Name = "Archived", DisplayOrder = 2, Status = UserStatus.Deleted });
        dbContext.LearningPurposes.Add(new LearningPurpose { LearningPurposeId = 1, Name = "Work", Status = UserStatus.Active });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRecommendationDataAsync(VocaNovaDbContext dbContext)
    {
        await SeedLookupsAsync(dbContext);
        dbContext.Users.AddRange(CreateUser(1), CreateUser(2), CreateUser(3));
        dbContext.UserLearningProfiles.AddRange(
            CreateProfile(1, 1, 1, 1, 1, 1),
            CreateProfile(2, 1, 1, 1, 1, 1),
            CreateProfile(3, 1, 1, 1, 1, 1));
        dbContext.Topics.AddRange(
            CreateTopic(100, "Current"),
            CreateTopic(101, "Travel"),
            CreateTopic(102, "Business"));
        dbContext.Words.AddRange(
            CreateWord(1),
            CreateWord(2),
            CreateWord(3));
        dbContext.WordTopics.AddRange(
            new WordTopic { WordId = 1, TopicId = 100 },
            new WordTopic { WordId = 2, TopicId = 101 },
            new WordTopic { WordId = 3, TopicId = 102 });
        dbContext.UserTopicPreferences.AddRange(
            CreatePreference(1, 100, TopicPreferenceSource.UserSelected),
            CreatePreference(2, 100, TopicPreferenceSource.UserSelected),
            CreatePreference(2, 101, TopicPreferenceSource.UserSelected),
            CreatePreference(3, 102, TopicPreferenceSource.KnnSuggested));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedFallbackDataAsync(VocaNovaDbContext dbContext)
    {
        await SeedLookupsAsync(dbContext);
        dbContext.Users.AddRange(CreateUser(1), CreateUser(2), CreateUser(3), CreateUser(4));
        dbContext.UserLearningProfiles.Add(new UserLearningProfile
        {
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        dbContext.Topics.AddRange(
            CreateTopic(101, "Travel"),
            CreateTopic(102, "Business"),
            CreateTopic(103, "Excluded"));
        dbContext.UserTopicPreferences.AddRange(
            CreatePreference(1, 103, TopicPreferenceSource.UserSelected),
            CreatePreference(2, 101, TopicPreferenceSource.UserSelected),
            CreatePreference(3, 101, TopicPreferenceSource.Onboarding),
            CreatePreference(4, 102, TopicPreferenceSource.UserSelected));

        await dbContext.SaveChangesAsync();
    }

    private static User CreateUser(uint userId)
    {
        return new User
        {
            UserId = userId,
            RoleId = 1,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static UserLearningProfile CreateProfile(
        uint userId,
        uint ageRangeId,
        uint regionId,
        uint occupationId,
        uint educationLevelId,
        uint learningPurposeId)
    {
        return new UserLearningProfile
        {
            UserId = userId,
            AgeRangeId = ageRangeId,
            RegionId = regionId,
            OccupationId = occupationId,
            EducationLevelId = educationLevelId,
            LearningPurposeId = learningPurposeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static Topic CreateTopic(uint topicId, string name)
    {
        return new Topic
        {
            TopicId = topicId,
            TopicName = name,
            Status = UserStatus.Active,
        };
    }

    private static Word CreateWord(uint wordId)
    {
        return new Word
        {
            WordId = wordId,
            Word1 = $"word-{wordId}",
            WordKey = $"word-{wordId}",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static UserTopicPreference CreatePreference(uint userId, uint topicId, string source)
    {
        return new UserTopicPreference
        {
            UserId = userId,
            TopicId = topicId,
            Source = source,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private sealed class FakeKnnTopicRecommendationCache : IKnnTopicRecommendationCache
    {
        private readonly IReadOnlyCollection<TopicRecommendationDto>? _cachedRecommendations;

        public FakeKnnTopicRecommendationCache(IReadOnlyCollection<TopicRecommendationDto>? cachedRecommendations = null)
        {
            _cachedRecommendations = cachedRecommendations;
        }

        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public TimeSpan? LastTtl { get; private set; }

        public Task<IReadOnlyCollection<TopicRecommendationDto>?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(_cachedRecommendations);
        }

        public Task SetAsync(
            uint userId,
            IReadOnlyCollection<TopicRecommendationDto> recommendations,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            LastTtl = ttl;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }
}
