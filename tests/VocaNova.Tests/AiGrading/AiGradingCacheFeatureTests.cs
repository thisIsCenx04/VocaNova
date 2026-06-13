using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.AiGrading.Repositories;
using VocaNova.API.Features.AiGrading.Services;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.AiGrading;

public class AiGradingCacheFeatureTests
{
    [Fact]
    public async Task GradeAsync_Should_Return_Cached_Result_And_Increment_HitCount_When_Cache_Valid()
    {
        await using var dbContext = CreateDbContext();
        var cacheKey = CachedAiGradingService.CreateCacheKey(10, 1, "Hello!!!".NormalizeAnswer());
        dbContext.AiGradingCaches.Add(new AiGradingCache
        {
            CacheId = 1,
            CacheKey = cacheKey,
            WordId = 10,
            QuestionType = 1,
            UserAnswerNormalized = "hello",
            ExpectedAnswer = "hello",
            AiScore = 0.9f,
            AiExplanation = "Cached explanation.",
            AiSuggestion = "Cached suggestion.",
            HitCount = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        await dbContext.SaveChangesAsync();
        var provider = new FakeAiGradingProvider();
        var service = CreateService(dbContext, provider);

        var result = await service.GradeAsync(10, 1, " Hello!!! ", "hello");

        result.IsCorrect.Should().BeTrue();
        result.Score.Should().Be(0.9f);
        result.Explanation.Should().Be("Cached explanation.");
        result.Suggestion.Should().Be("Cached suggestion.");
        provider.CallCount.Should().Be(0);

        var cache = await dbContext.AiGradingCaches.SingleAsync();
        cache.HitCount.Should().Be(3);
    }

    [Fact]
    public async Task GradeAsync_Should_Call_Provider_And_Refresh_Cache_When_Cache_Expired()
    {
        await using var dbContext = CreateDbContext();
        var cacheKey = CachedAiGradingService.CreateCacheKey(10, 1, "hello");
        dbContext.AiGradingCaches.Add(new AiGradingCache
        {
            CacheId = 1,
            CacheKey = cacheKey,
            WordId = 10,
            QuestionType = 1,
            UserAnswerNormalized = "hello",
            ExpectedAnswer = "hello",
            AiScore = 0.9f,
            AiExplanation = "Expired explanation.",
            HitCount = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        });
        await dbContext.SaveChangesAsync();
        var provider = new FakeAiGradingProvider(new AiGradingResult(
            false,
            0.25f,
            "Provider explanation.",
            "Provider suggestion."));
        var service = CreateService(dbContext, provider);

        var result = await service.GradeAsync(10, 1, "hello", "hello");

        result.IsCorrect.Should().BeFalse();
        result.Score.Should().Be(0.25f);
        result.Explanation.Should().Be("Provider explanation.");
        result.Suggestion.Should().Be("Provider suggestion.");
        provider.CallCount.Should().Be(1);

        var cache = await dbContext.AiGradingCaches.SingleAsync();
        cache.HitCount.Should().Be(1);
        cache.AiScore.Should().Be(0.25f);
        cache.AiExplanation.Should().Be("Provider explanation.");
        cache.AiSuggestion.Should().Be("Provider suggestion.");
        cache.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task GradeAsync_Should_Save_New_Cache_Row_When_Cache_Missing()
    {
        await using var dbContext = CreateDbContext();
        var provider = new FakeAiGradingProvider(new AiGradingResult(
            true,
            0.95f,
            "New provider explanation.",
            null));
        var service = CreateService(dbContext, provider);

        var result = await service.GradeAsync(30, 3, "close answer", "expected answer");

        result.Score.Should().Be(0.95f);
        provider.CallCount.Should().Be(1);

        var cache = await dbContext.AiGradingCaches.SingleAsync();
        cache.CacheKey.Should().Be(CachedAiGradingService.CreateCacheKey(30, 3, "close answer"));
        cache.WordId.Should().Be(30);
        cache.QuestionType.Should().Be(3);
        cache.UserAnswerNormalized.Should().Be("close answer");
        cache.ExpectedAnswer.Should().Be("expected answer");
        cache.AiScore.Should().Be(0.95f);
        cache.HitCount.Should().Be(1);
        cache.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task GradeAsync_Should_Normalize_UserAnswer_For_Cache_Key()
    {
        await using var dbContext = CreateDbContext();
        var cacheKey = CachedAiGradingService.CreateCacheKey(20, 2, "move fast");
        dbContext.AiGradingCaches.Add(new AiGradingCache
        {
            CacheId = 1,
            CacheKey = cacheKey,
            WordId = 20,
            QuestionType = 2,
            UserAnswerNormalized = "move fast",
            ExpectedAnswer = "move fast",
            AiScore = 1f,
            AiExplanation = "Normalized hit.",
            HitCount = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        await dbContext.SaveChangesAsync();
        var provider = new FakeAiGradingProvider();
        var service = CreateService(dbContext, provider);

        var result = await service.GradeAsync(20, 2, " MOVE FAST!!! ", "move fast");

        result.Score.Should().Be(1f);
        result.Explanation.Should().Be("Normalized hit.");
        provider.CallCount.Should().Be(0);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static CachedAiGradingService CreateService(
        VocaNovaDbContext dbContext,
        FakeAiGradingProvider provider)
    {
        return new CachedAiGradingService(
            new AiGradingCacheRepository(dbContext),
            provider);
    }

    private sealed class FakeAiGradingProvider : IAiGradingProvider
    {
        private readonly AiGradingResult _result;

        public FakeAiGradingProvider(AiGradingResult? result = null)
        {
            _result = result ?? new AiGradingResult(true, 1f, "Provider called.", null);
        }

        public int CallCount { get; private set; }

        public Task<AiGradingResult> GradeAsync(
            uint wordId,
            int questionType,
            string? userAnswer,
            string expectedAnswer,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
