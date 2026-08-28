using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Progress.DAL.Repositories;
using VocaNova.API.Features.Progress.BLL.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using UserWordProgressEntity = VocaNova.API.Infrastructure.Persistence.Entities.UserWordProgress;

namespace VocaNova.Tests.Progress;

public class ProgressAnalyticsFeatureTests
{
    [Fact]
    public async Task GetChartAsync_Should_Return_Daily_Buckets_With_Session_And_Accuracy()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAnalyticsAsync(dbContext, today);
        var service = CreateService(dbContext);

        var result = await service.GetChartAsync(1, new ProgressChartQuery("daily"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Granularity.Should().Be("daily");
        result.Value.Points.Should().HaveCount(30);

        var todayPoint = result.Value.Points.Single(point => point.PeriodStart == today);
        todayPoint.SessionsCount.Should().Be(1);
        todayPoint.CorrectCount.Should().Be(2);
        todayPoint.TotalAnswers.Should().Be(3);
        todayPoint.Accuracy.Should().BeApproximately(66.666f, 0.01f);

        result.Value.Points.Should().NotContain(point => point.PeriodStart == today.AddDays(-31));
    }

    [Theory]
    [InlineData("weekly", 12)]
    [InlineData("monthly", 6)]
    public async Task GetChartAsync_Should_Return_Expected_Bucket_Count_For_Granularity(
        string granularity,
        int expectedCount)
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAnalyticsAsync(dbContext, today);
        var service = CreateService(dbContext);

        var result = await service.GetChartAsync(1, new ProgressChartQuery(granularity));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Granularity.Should().Be(granularity);
        result.Value.Points.Should().HaveCount(expectedCount);
        result.Value.Points.Sum(point => point.SessionsCount).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetChartAsync_Should_Reject_Invalid_Granularity()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetChartAsync(1, new ProgressChartQuery("yearly"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetMasteryBreakdownAsync_Should_Return_Levels_Zero_To_Five()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAnalyticsAsync(dbContext, today);
        var service = CreateService(dbContext);

        var result = await service.GetMasteryBreakdownAsync(1);

        result.IsSuccess.Should().BeTrue();
        var breakdown = result.Value!;
        breakdown.Select(item => item.MasteryLevel).Should().Equal(0, 1, 2, 3, 4, 5);
        breakdown.Single(item => item.MasteryLevel == 0).WordCount.Should().Be(1);
        breakdown.Single(item => item.MasteryLevel == 2).WordCount.Should().Be(1);
        breakdown.Single(item => item.MasteryLevel == 5).WordCount.Should().Be(1);
        breakdown.Sum(item => item.WordCount).Should().Be(3);
    }

    [Fact]
    public async Task GetWeakestWordsAsync_Should_Filter_Flagged_Words_And_Sort_By_WrongCount()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAnalyticsAsync(dbContext, today);
        var service = CreateService(dbContext);

        var result = await service.GetWeakestWordsAsync(1, new WeakestWordsQuery(2));

        result.IsSuccess.Should().BeTrue();
        var weakestWords = result.Value!;
        weakestWords.Select(item => item.WordId).Should().Equal(2u, 1u);
        weakestWords.First().WrongCount.Should().Be(7);
        weakestWords.First().AccuracyRate.Should().BeApproximately(30f, 0.01f);
        weakestWords.Should().OnlyContain(item => item.WordId != 3 && item.WordId != 4);
    }

    [Fact]
    public async Task GetWeakestWordsAsync_Should_Reject_Invalid_Limit()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetWeakestWordsAsync(1, new WeakestWordsQuery(0));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetWordProgressAsync_Should_Return_Detail_For_Current_User()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAnalyticsAsync(dbContext, today);
        var service = CreateService(dbContext);

        var result = await service.GetWordProgressAsync(1, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordId.Should().Be(2);
        result.Value.Word.Should().Be("walk");
        result.Value.PrimaryMeaning.Should().Be("di bo");
        result.Value.TestCount.Should().Be(10);
        result.Value.WrongCount.Should().Be(7);
        result.Value.IsInWrongList.Should().BeTrue();
    }

    [Fact]
    public async Task GetWordProgressAsync_Should_Return_NotFound_When_Progress_Missing()
    {
        await using var dbContext = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAnalyticsAsync(dbContext, today);
        var service = CreateService(dbContext);

        var result = await service.GetWordProgressAsync(1, 4);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static ProgressAnalyticsService CreateService(VocaNovaDbContext dbContext)
    {
        return new ProgressAnalyticsService(new ProgressAnalyticsRepository(dbContext));
    }

    private static async Task SeedAnalyticsAsync(VocaNovaDbContext dbContext, DateOnly today)
    {
        var now = today.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);

        dbContext.Users.AddRange(
            CreateUser(1, now),
            CreateUser(2, now));

        AddWord(dbContext, 1, "run", "chay", now);
        AddWord(dbContext, 2, "walk", "di bo", now);
        AddWord(dbContext, 3, "jump", "nhay", now);
        AddWord(dbContext, 4, "fly", "bay", now);

        dbContext.TestSessions.AddRange(
            CreateSession(
                1,
                1,
                today.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
                new[]
                {
                    CreateAnswer(1, 1, 1, isCorrect: true),
                    CreateAnswer(2, 1, 2, isCorrect: true),
                    CreateAnswer(3, 1, 3, isCorrect: false),
                }),
            CreateSession(
                2,
                1,
                today.AddDays(-1).ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
                new[]
                {
                    CreateAnswer(4, 2, 1, isCorrect: false),
                }),
            CreateSession(
                3,
                1,
                today.AddDays(-31).ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
                new[]
                {
                    CreateAnswer(5, 3, 1, isCorrect: true),
                }),
            CreateSession(
                4,
                2,
                today.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
                new[]
                {
                    CreateAnswer(6, 4, 4, isCorrect: true),
                }));

        dbContext.UserWordProgresses.AddRange(
            CreateProgress(1, 1, 1, testCount: 5, correct: 3, wrong: 2, isWrong: true, mastery: 0, now),
            CreateProgress(2, 1, 2, testCount: 10, correct: 3, wrong: 7, isWrong: true, mastery: 2, now),
            CreateProgress(3, 1, 3, testCount: 8, correct: 8, wrong: 0, isWrong: false, mastery: 5, now),
            CreateProgress(4, 2, 4, testCount: 20, correct: 1, wrong: 19, isWrong: true, mastery: 1, now));

        await dbContext.SaveChangesAsync();
    }

    private static User CreateUser(uint userId, DateTime now)
    {
        return new User
        {
            UserId = userId,
            RoleId = 1,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static void AddWord(
        VocaNovaDbContext dbContext,
        uint wordId,
        string wordText,
        string meaning,
        DateTime now)
    {
        dbContext.Words.Add(new Word
        {
            WordId = wordId,
            Word1 = wordText,
            WordKey = wordText,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            WordSenses =
            {
                new EntityWordSense
                {
                    SenseId = wordId,
                    WordId = wordId,
                    SenseOrder = 1,
                    WordClass = "verb",
                    EnglishDefinition = $"{wordText} definition",
                    VietnameseMeaning = meaning,
                },
            },
        });
    }

    private static TestSession CreateSession(
        uint sessionId,
        uint userId,
        DateTime startedAt,
        IReadOnlyCollection<TestAnswer> answers)
    {
        var correct = answers.Count(answer => answer.IsCorrect == true);
        var wrong = answers.Count(answer => answer.IsCorrect == false);

        return new TestSession
        {
            SessionId = sessionId,
            UserId = userId,
            TestType = AnswerMethod.ExactTyping,
            Mode = TestMode.Standard,
            QuestionType = 1,
            ScopeType = ScopeType.All,
            WordOrder = WordOrder.Newest,
            QuestionCount = answers.Count,
            CorrectCount = correct,
            WrongCount = wrong,
            Score = answers.Count == 0 ? 0 : (float)correct / answers.Count * 100,
            MaxStreak = correct,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(5),
            Status = TestSessionStatus.Completed,
            TestAnswers = answers.ToList(),
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

    private static UserWordProgressEntity CreateProgress(
        uint progressId,
        uint userId,
        uint wordId,
        int testCount,
        int correct,
        int wrong,
        bool isWrong,
        int mastery,
        DateTime now)
    {
        return new UserWordProgressEntity
        {
            ProgressId = progressId,
            UserId = userId,
            WordId = wordId,
            TestCount = testCount,
            CorrectCount = correct,
            WrongCount = wrong,
            ConsecutiveCorrect = correct,
            IsInWrongList = isWrong,
            MasteryLevel = mastery,
            SrsInterval = 1,
            EaseFactor = 2.5f,
            LastTestedAt = now,
            LastWrongAt = isWrong ? now.AddMinutes(wrong) : null,
            NextReviewAt = now.AddDays(1),
            UpdatedAt = now,
        };
    }
}
