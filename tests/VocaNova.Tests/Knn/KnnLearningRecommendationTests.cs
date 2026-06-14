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

namespace VocaNova.Tests.Knn;

public class KnnLearningRecommendationTests
{
    [Fact]
    public async Task ComputeTopicAccuracyVectorAsync_Should_Return_Fail_When_User_Has_Too_Few_Sessions()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        dbContext.Users.Add(CreateUser(1));
        dbContext.TestSessions.Add(CreateSession(1, 1));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ComputeTopicAccuracyVectorAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Not enough sessions for KNN learning recommendation.");
    }

    [Fact]
    public async Task ComputeTopicAccuracyVectorAsync_Should_Calculate_Accuracy_For_Each_Active_Topic()
    {
        await using var dbContext = CreateDbContext();
        await SeedAccuracyDataAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.ComputeTopicAccuracyVectorAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(0.5, 1.0, 0.0);
    }

    [Fact]
    public void CosineSimilarity_Should_Return_Zero_When_Any_Vector_Is_AllZero()
    {
        KnnMathHelper.CosineSimilarity(new[] { 0.0, 0.0 }, new[] { 1.0, 0.0 })
            .Should()
            .Be(0.0);
    }

    [Fact]
    public async Task GenerateWordRecommendationsAsync_Should_Filter_UserOwned_Words_And_Save_RedisSnapshot()
    {
        await using var dbContext = CreateDbContext();
        await SeedGenerationDataAsync(dbContext);
        var cache = new FakeKnnWordRecommendationCache();
        var service = CreateService(dbContext, cache);

        var result = await service.GenerateWordRecommendationsAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(word => word.WordId).Should().Equal(202u);
        result.Value!.Single().Score.Should().BeApproximately(1.0, 0.0001);
        cache.SetCount.Should().Be(1);
        cache.StoredRecommendations!.Select(word => word.WordId).Should().Equal(202u);
        cache.LastTtl.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task GetWordRecommendationsAsync_Should_Return_Empty_When_RedisMiss()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new FakeKnnWordRecommendationCache());

        var result = await service.GetWordRecommendationsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWordRecommendationsAsync_Should_Read_Cache_And_Join_Latest_Word_Info()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Words.Add(CreateWord(301, "fresh-word", "B2", "/fresh/", "https://example.com/fresh.png"));
        dbContext.WordSenses.Add(new WordSense
        {
            SenseId = 301,
            WordId = 301,
            SenseOrder = 1,
            WordClass = "noun",
            EnglishDefinition = "fresh definition",
            VietnameseMeaning = "fresh meaning",
        });
        await dbContext.SaveChangesAsync();
        var cache = new FakeKnnWordRecommendationCache(new[]
        {
            new WordRecommendationItem(301, "stale", null, null, null, null, 0.77),
        });
        var service = CreateService(dbContext, cache);

        var result = await service.GetWordRecommendationsAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var recommendation = result.Value!.Single();
        recommendation.WordId.Should().Be(301);
        recommendation.Word.Should().Be("fresh-word");
        recommendation.PhoneticUk.Should().Be("/fresh/");
        recommendation.PrimaryMeaning.Should().Be("fresh meaning");
        recommendation.ImageUrl.Should().Be("https://example.com/fresh.png");
        recommendation.CefrLevel.Should().Be("B2");
        recommendation.Score.Should().Be(0.77);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static KnnLearningService CreateService(
        VocaNovaDbContext dbContext,
        IKnnWordRecommendationCache? cache = null)
    {
        return new KnnLearningService(
            new KnnLearningRepository(dbContext),
            Options.Create(CreateOptions()),
            cache);
    }

    private static KnnOptions CreateOptions()
    {
        return new KnnOptions
        {
            Learning = new KnnLearningOptions
            {
                KValue = 5,
                MinSessions = 2,
                MinSimilarity = 0.1,
                RecommendationCount = 50,
                RebuildIntervalHours = 24,
                CacheTtlMinutes = 60,
            },
        };
    }

    private static async Task SeedTopicsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Topics.AddRange(
            CreateTopic(100, "Travel"),
            CreateTopic(101, "Business"),
            CreateTopic(102, "Food"));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAccuracyDataAsync(VocaNovaDbContext dbContext)
    {
        await SeedTopicsAsync(dbContext);
        dbContext.Users.Add(CreateUser(1));
        dbContext.Words.AddRange(
            CreateWord(1, "word-1"),
            CreateWord(2, "word-2"),
            CreateWord(3, "word-3"));
        dbContext.WordTopics.AddRange(
            new WordTopic { WordId = 1, TopicId = 100 },
            new WordTopic { WordId = 2, TopicId = 100 },
            new WordTopic { WordId = 3, TopicId = 101 });
        dbContext.TestSessions.AddRange(CreateSession(1, 1), CreateSession(2, 1));
        dbContext.TestAnswers.AddRange(
            CreateAnswer(1, 1, 1, true),
            CreateAnswer(2, 1, 2, false),
            CreateAnswer(3, 2, 3, true));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedGenerationDataAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Topics.AddRange(
            CreateTopic(100, "Travel"),
            CreateTopic(101, "Business"));
        dbContext.Users.AddRange(CreateUser(1), CreateUser(2), CreateUser(3));
        dbContext.Words.AddRange(
            CreateWord(1, "travel-user"),
            CreateWord(2, "business-user"),
            CreateWord(3, "travel-neighbor"),
            CreateWord(4, "business-neighbor"),
            CreateWord(201, "owned-word", "A2", "/owned/", null),
            CreateWord(202, "recommended-word", "B1", "/recommended/", "https://example.com/recommended.png"));
        dbContext.WordSenses.Add(new WordSense
        {
            SenseId = 202,
            WordId = 202,
            SenseOrder = 1,
            WordClass = "verb",
            EnglishDefinition = "recommended definition",
            VietnameseMeaning = "recommended meaning",
        });
        dbContext.WordTopics.AddRange(
            new WordTopic { WordId = 1, TopicId = 100 },
            new WordTopic { WordId = 2, TopicId = 101 },
            new WordTopic { WordId = 3, TopicId = 100 },
            new WordTopic { WordId = 4, TopicId = 101 });

        dbContext.TestSessions.AddRange(
            CreateSession(1, 1),
            CreateSession(2, 1),
            CreateSession(3, 2),
            CreateSession(4, 2),
            CreateSession(5, 3),
            CreateSession(6, 3));
        dbContext.TestAnswers.AddRange(
            CreateAnswer(1, 1, 1, true),
            CreateAnswer(2, 2, 1, true),
            CreateAnswer(3, 3, 3, true),
            CreateAnswer(4, 4, 3, true),
            CreateAnswer(5, 5, 4, true),
            CreateAnswer(6, 6, 4, true));
        dbContext.UserWordProgresses.AddRange(
            CreateProgress(1, 2, 201, 5),
            CreateProgress(2, 2, 202, 3),
            CreateProgress(3, 3, 202, 3));
        dbContext.UserListWords.Add(new UserListWord
        {
            UserId = 1,
            ListId = 1,
            WordId = 201,
            AddMethod = AddMethod.Manual,
            Status = UserStatus.Active,
            AddedAt = DateTime.UtcNow,
        });

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

    private static Topic CreateTopic(uint topicId, string name)
    {
        return new Topic
        {
            TopicId = topicId,
            TopicName = name,
            Status = UserStatus.Active,
        };
    }

    private static Word CreateWord(
        uint wordId,
        string word,
        string? cefr = null,
        string? phoneticUk = null,
        string? imageUrl = null)
    {
        return new Word
        {
            WordId = wordId,
            Word1 = word,
            WordKey = word,
            CefrLevel = cefr,
            PhoneticUk = phoneticUk,
            ImageUrl = imageUrl,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static TestSession CreateSession(uint sessionId, uint userId)
    {
        return new TestSession
        {
            SessionId = sessionId,
            UserId = userId,
            TestType = AnswerMethod.ExactTyping,
            Mode = TestMode.Standard,
            QuestionType = 1,
            ScopeType = ScopeType.All,
            WordOrder = WordOrder.Newest,
            QuestionCount = 1,
            CorrectCount = 0,
            WrongCount = 0,
            Score = 0,
            MaxStreak = 0,
            StartedAt = DateTime.UtcNow,
            Status = TestSessionStatus.Completed,
        };
    }

    private static TestAnswer CreateAnswer(uint answerId, uint sessionId, uint wordId, bool isCorrect)
    {
        return new TestAnswer
        {
            AnswerId = answerId,
            SessionId = sessionId,
            WordId = wordId,
            QuestionNumber = (int)answerId,
            QuestionType = 1,
            DisplayContent = $"word-{wordId}",
            ExpectedAnswer = $"meaning-{wordId}",
            UserAnswer = $"meaning-{wordId}",
            IsCorrect = isCorrect,
        };
    }

    private static UserWordProgress CreateProgress(
        uint progressId,
        uint userId,
        uint wordId,
        int masteryLevel)
    {
        return new UserWordProgress
        {
            ProgressId = progressId,
            UserId = userId,
            WordId = wordId,
            TestCount = 1,
            CorrectCount = 1,
            WrongCount = 0,
            ConsecutiveCorrect = 1,
            IsInWrongList = false,
            MasteryLevel = masteryLevel,
            SrsInterval = 1,
            EaseFactor = 2.5f,
            LastTestedAt = DateTime.UtcNow,
            NextReviewAt = DateTime.UtcNow.AddDays(1),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private sealed class FakeKnnWordRecommendationCache : IKnnWordRecommendationCache
    {
        private readonly IReadOnlyCollection<WordRecommendationItem>? _cachedRecommendations;

        public FakeKnnWordRecommendationCache(
            IReadOnlyCollection<WordRecommendationItem>? cachedRecommendations = null)
        {
            _cachedRecommendations = cachedRecommendations;
        }

        public int SetCount { get; private set; }

        public TimeSpan? LastTtl { get; private set; }

        public IReadOnlyCollection<WordRecommendationItem>? StoredRecommendations { get; private set; }

        public Task<IReadOnlyCollection<WordRecommendationItem>?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_cachedRecommendations);
        }

        public Task SetAsync(
            uint userId,
            IReadOnlyCollection<WordRecommendationItem> recommendations,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            StoredRecommendations = recommendations;
            LastTtl = ttl;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
