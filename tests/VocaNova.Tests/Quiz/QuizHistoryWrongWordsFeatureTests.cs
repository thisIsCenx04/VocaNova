using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.Contracts.Requests;
using VocaNova.API.Features.Quiz.Contracts.Responses;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.DAL.Repositories;
using VocaNova.API.Features.Quiz.BLL.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Quiz;

public class QuizHistoryWrongWordsFeatureTests
{
    [Fact]
    public async Task GetHistoryAsync_Should_Return_User_Sessions_Newest_First()
    {
        await using var dbContext = CreateDbContext();
        await SeedHistoryAndWrongWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(1, new QuizHistoryQuery(1, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(item => item.SessionId).Should().Equal(2u, 1u);
        result.Value.Items.Should().OnlyContain(item => item.SessionId != 3);
    }

    [Fact]
    public async Task GetWrongWordsAsync_Should_Return_Only_Flagged_Words_Sorted_By_WrongCount()
    {
        await using var dbContext = CreateDbContext();
        await SeedHistoryAndWrongWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetWrongWordsAsync(1, new WrongWordsQuery(1, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(item => item.WordId).Should().Equal(2u, 1u);
        result.Value.Items.Should().OnlyContain(item => item.WordId != 3 && item.WordId != 4);
        result.Value.Items.First().WrongCount.Should().Be(5);
    }

    [Fact]
    public async Task ClearWrongWordAsync_Should_Clear_Flag_Without_Deleting_Record()
    {
        await using var dbContext = CreateDbContext();
        await SeedHistoryAndWrongWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.ClearWrongWordAsync(1, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var progress = await dbContext.UserWordProgresses.SingleAsync(entity => entity.UserId == 1 && entity.WordId == 2);
        progress.IsInWrongList.Should().BeFalse();
        (await dbContext.UserWordProgresses.CountAsync()).Should().Be(4);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static QuizHistoryService CreateService(VocaNovaDbContext dbContext)
    {
        return new QuizHistoryService(new QuizHistoryRepository(dbContext));
    }

    private static async Task SeedHistoryAndWrongWordsAsync(VocaNovaDbContext dbContext)
    {
        var now = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        dbContext.Users.AddRange(
            new User
            {
                UserId = 1,
                RoleId = 1,
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new User
            {
                UserId = 2,
                RoleId = 1,
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            });

        dbContext.TestSessions.AddRange(
            CreateSession(1, 1, now.AddDays(-2), TestSessionStatus.Completed, correct: 3, wrong: 1),
            CreateSession(2, 1, now.AddDays(-1), TestSessionStatus.Abandoned, correct: 1, wrong: 2),
            CreateSession(3, 2, now, TestSessionStatus.Completed, correct: 9, wrong: 0));

        AddWordWithProgress(dbContext, 1, "run", "chay", userId: 1, wrongCount: 2, isWrong: true, now);
        AddWordWithProgress(dbContext, 2, "walk", "di bo", userId: 1, wrongCount: 5, isWrong: true, now);
        AddWordWithProgress(dbContext, 3, "jump", "nhay", userId: 1, wrongCount: 9, isWrong: false, now);
        AddWordWithProgress(dbContext, 4, "fly", "bay", userId: 2, wrongCount: 10, isWrong: true, now);

        await dbContext.SaveChangesAsync();
    }

    private static TestSession CreateSession(
        uint sessionId,
        uint userId,
        DateTime startedAt,
        string status,
        int correct,
        int wrong)
    {
        var answered = correct + wrong;
        return new TestSession
        {
            SessionId = sessionId,
            UserId = userId,
            TestType = AnswerMethod.ExactTyping,
            Mode = TestMode.Standard,
            QuestionType = 1,
            ScopeType = ScopeType.All,
            WordOrder = WordOrder.Newest,
            QuestionCount = answered,
            CorrectCount = correct,
            WrongCount = wrong,
            Score = answered == 0 ? 0 : (float)correct / answered * 100,
            MaxStreak = correct,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(5),
            Status = status,
        };
    }

    private static void AddWordWithProgress(
        VocaNovaDbContext dbContext,
        uint wordId,
        string wordText,
        string meaning,
        uint userId,
        int wrongCount,
        bool isWrong,
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

        dbContext.UserWordProgresses.Add(new EntityUserWordProgress
        {
            ProgressId = wordId,
            UserId = userId,
            WordId = wordId,
            TestCount = wrongCount + 1,
            CorrectCount = 1,
            WrongCount = wrongCount,
            ConsecutiveCorrect = 0,
            IsInWrongList = isWrong,
            MasteryLevel = 1,
            SrsInterval = 1,
            EaseFactor = 2.5f,
            LastWrongAt = now.AddMinutes(wrongCount),
            NextReviewAt = now.AddDays(1),
            UpdatedAt = now,
        });
    }
}
