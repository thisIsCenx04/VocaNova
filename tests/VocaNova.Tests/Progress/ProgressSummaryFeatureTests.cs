using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Progress.DTOs;
using VocaNova.API.Features.Progress.Repositories;
using VocaNova.API.Features.Progress.Services;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Progress;

public class ProgressSummaryFeatureTests
{
    [Fact]
    public async Task GetSummaryAsync_Should_Calculate_Streak_And_SevenDay_Accuracy()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedSummaryAsync(
            dbContext,
            today,
            sessionDates: new[] { today, today.AddDays(-1), today.AddDays(-3) });
        var cache = new FakeProgressSummaryCache();
        var service = CreateService(dbContext, cache);

        var result = await service.GetSummaryAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentStreakDays.Should().Be(2);
        result.Value.LongestStreakDays.Should().Be(2);
        result.Value.Correct7Days.Should().Be(2);
        result.Value.TotalAnswers7Days.Should().Be(3);
        result.Value.Accuracy7Days.Should().BeApproximately(66.666f, 0.01f);
        result.Value.TotalWordsInProgress.Should().Be(3);
        result.Value.MasteredWords.Should().Be(1);
        result.Value.SessionsThisMonth.Should().BeGreaterThan(0);
        cache.SetCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_Should_Keep_Current_Streak_When_Today_Has_No_Session()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedSummaryAsync(
            dbContext,
            today,
            sessionDates: new[] { today.AddDays(-1), today.AddDays(-2) });
        var service = CreateService(dbContext);

        var result = await service.GetSummaryAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentStreakDays.Should().Be(2);
        result.Value.LongestStreakDays.Should().Be(2);
    }

    [Fact]
    public async Task GetSummaryAsync_Should_Return_Zero_Current_Streak_When_Latest_Session_Is_Before_Yesterday()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedSummaryAsync(
            dbContext,
            today,
            sessionDates: new[] { today.AddDays(-2), today.AddDays(-3) },
            includeAnswerSessions: false);
        var service = CreateService(dbContext);

        var result = await service.GetSummaryAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentStreakDays.Should().Be(0);
        result.Value.LongestStreakDays.Should().Be(2);
    }

    [Fact]
    public async Task GetSummaryAsync_Should_Return_Cached_Summary_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cachedSummary = new ProgressSummaryDto(7, 9, 88, 22, 25, 30, 4, 6);
        var cache = new FakeProgressSummaryCache(cachedSummary);
        var service = CreateService(dbContext, cache);

        var result = await service.GetSummaryAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedSummary);
        cache.GetCount.Should().Be(1);
        cache.SetCount.Should().Be(0);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static ProgressSummaryService CreateService(
        VocaNovaDbContext dbContext,
        IProgressSummaryCache? cache = null)
    {
        return new ProgressSummaryService(new ProgressSummaryRepository(dbContext), cache);
    }

    private static async Task SeedSummaryAsync(
        VocaNovaDbContext dbContext,
        DateOnly today,
        IReadOnlyCollection<DateOnly> sessionDates,
        bool includeAnswerSessions = true)
    {
        var now = today.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);

        dbContext.Users.Add(new User
        {
            UserId = 1,
            RoleId = 1,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        });

        var sessionId = 1u;
        foreach (var date in sessionDates)
        {
            var session = CreateSession(sessionId++, 1, date.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc));
            dbContext.TestSessions.Add(session);
        }

        if (includeAnswerSessions)
        {
            var answeredSession = CreateSession(100, 1, today.AddDays(-1).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc));
            answeredSession.TestAnswers.Add(CreateAnswer(1, 100, 1, isCorrect: true));
            answeredSession.TestAnswers.Add(CreateAnswer(2, 100, 2, isCorrect: true));
            answeredSession.TestAnswers.Add(CreateAnswer(3, 100, 3, isCorrect: false));
            dbContext.TestSessions.Add(answeredSession);

            var oldSession = CreateSession(101, 1, today.AddDays(-8).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc));
            oldSession.TestAnswers.Add(CreateAnswer(4, 101, 4, isCorrect: true));
            dbContext.TestSessions.Add(oldSession);
        }

        dbContext.UserWordProgresses.AddRange(
            CreateProgress(1, 1, masteryLevel: 1, now),
            CreateProgress(2, 2, masteryLevel: 5, now),
            CreateProgress(3, 3, masteryLevel: 4, now));

        await dbContext.SaveChangesAsync();
    }

    private static TestSession CreateSession(uint sessionId, uint userId, DateTime startedAt)
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
            QuestionCount = 3,
            CorrectCount = 0,
            WrongCount = 0,
            Score = 0,
            MaxStreak = 0,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(5),
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
        uint wordId,
        int masteryLevel,
        DateTime now)
    {
        return new UserWordProgress
        {
            ProgressId = progressId,
            UserId = 1,
            WordId = wordId,
            TestCount = 1,
            CorrectCount = 1,
            WrongCount = 0,
            ConsecutiveCorrect = 1,
            IsInWrongList = false,
            MasteryLevel = masteryLevel,
            SrsInterval = 1,
            EaseFactor = 2.5f,
            LastTestedAt = now,
            NextReviewAt = now.AddDays(1),
            UpdatedAt = now,
        };
    }

    private sealed class FakeProgressSummaryCache : IProgressSummaryCache
    {
        private readonly ProgressSummaryDto? _cachedSummary;

        public FakeProgressSummaryCache(ProgressSummaryDto? cachedSummary = null)
        {
            _cachedSummary = cachedSummary;
        }

        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public Task<ProgressSummaryDto?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(_cachedSummary);
        }

        public Task SetAsync(
            uint userId,
            ProgressSummaryDto summary,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }
}
