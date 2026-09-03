using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.Contracts.Requests;
using VocaNova.API.Features.Quiz.Contracts.Responses;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.DAL.Repositories;
using VocaNova.API.Features.Quiz.BLL.Services;
using VocaNova.API.Features.Quiz.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Quiz;

public class QuizFinishResultFeatureTests
{
    [Fact]
    public async Task FinishSessionAsync_Should_Set_Abandoned_And_Calculate_Partial_Stats()
    {
        await using var dbContext = CreateDbContext();
        await SeedResultSessionAsync(dbContext, TestSessionStatus.InProgress);
        var service = CreateResultService(dbContext);

        var result = await service.FinishSessionAsync(1, 100);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(TestSessionStatus.Abandoned);
        result.Value.CorrectCount.Should().Be(2);
        result.Value.WrongCount.Should().Be(1);
        result.Value.AnsweredCount.Should().Be(3);
        // Accuracy is over answered questions (2 of 3); score is over the full quiz (2 of 5).
        result.Value.Accuracy.Should().BeApproximately(66.666f, 0.01f);
        result.Value.Score.Should().BeApproximately(40f, 0.01f);
        result.Value.MaxStreak.Should().Be(1);
        result.Value.DurationSec.Should().NotBeNull();

        var session = await dbContext.TestSessions.SingleAsync(entity => entity.SessionId == 100);
        session.Status.Should().Be(TestSessionStatus.Abandoned);
        session.EndedAt.Should().NotBeNull();
        session.Score.Should().BeApproximately(40f, 0.01f);
    }

    [Fact]
    public async Task GetResultAsync_Should_Return_Full_Session_Answers_And_Duration()
    {
        await using var dbContext = CreateDbContext();
        await SeedResultSessionAsync(dbContext, TestSessionStatus.Completed, endedAtOffsetSeconds: 125);
        var service = CreateResultService(dbContext);

        var result = await service.GetResultAsync(1, 100);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(TestSessionStatus.Completed);
        result.Value.DurationSec.Should().Be(125);
        // 2 correct of 3 answered = 66.6% accuracy, but 2 of 5 total questions = 40% score.
        result.Value.Accuracy.Should().BeApproximately(66.666f, 0.01f);
        result.Value.Score.Should().BeApproximately(40f, 0.01f);
        result.Value.Answers.Should().HaveCount(3);
        result.Value.Answers.Select(answer => answer.QuestionNumber).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_AutoComplete_When_Submitting_LastQuestion()
    {
        await using var dbContext = CreateDbContext();
        await SeedAutoCompleteSessionAsync(dbContext);
        var service = CreateSubmitService(dbContext);

        var result = await service.SubmitAnswerAsync(
            1,
            200,
            new SubmitAnswerRequest(4, "bay").ToBusinessCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.NextQuestion.Should().BeNull();

        var session = await dbContext.TestSessions.SingleAsync(entity => entity.SessionId == 200);
        session.Status.Should().Be(TestSessionStatus.Completed);
        session.EndedAt.Should().NotBeNull();
        session.CorrectCount.Should().Be(1);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static QuizResultService CreateResultService(VocaNovaDbContext dbContext)
    {
        return new QuizResultService(new QuizResultRepository(dbContext));
    }

    private static QuizSubmitService CreateSubmitService(VocaNovaDbContext dbContext)
    {
        return new QuizSubmitService(
            new QuizSubmitRepository(dbContext),
            new QuizSessionBuilder(new QuizWordPoolRepository(dbContext)),
            new QuizQuestionBuilder(new QuizQuestionRepository(dbContext)),
            new IAnswerGrader[]
            {
                new ExactTypingGrader(),
                new MultipleChoiceGrader(),
            },
            new StubAiGradingService(),
            new SrsService(new SrsRepository(dbContext)));
    }

    private static async Task SeedResultSessionAsync(
        VocaNovaDbContext dbContext,
        string status,
        int? endedAtOffsetSeconds = null)
    {
        var startedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        dbContext.Users.Add(new User
        {
            UserId = 1,
            RoleId = 1,
            Status = UserStatus.Active,
            CreatedAt = startedAt,
            UpdatedAt = startedAt,
        });

        dbContext.TestSessions.Add(new TestSession
        {
            SessionId = 100,
            UserId = 1,
            TestType = AnswerMethod.ExactTyping,
            Mode = TestMode.Standard,
            QuestionType = 1,
            ScopeType = ScopeType.All,
            WordOrder = WordOrder.Newest,
            QuestionCount = 5,
            CorrectCount = 0,
            WrongCount = 0,
            Score = 0,
            MaxStreak = 0,
            StartedAt = startedAt,
            EndedAt = endedAtOffsetSeconds.HasValue
                ? startedAt.AddSeconds(endedAtOffsetSeconds.Value)
                : null,
            Status = status,
            TestAnswers =
            {
                CreateAnswer(1, 1, true),
                CreateAnswer(2, 2, false),
                CreateAnswer(3, 3, true),
            },
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAutoCompleteSessionAsync(VocaNovaDbContext dbContext)
    {
        var now = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        dbContext.Users.Add(new User
        {
            UserId = 1,
            RoleId = 1,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        });

        dbContext.UserLists.Add(new UserList
        {
            ListId = 1,
            UserId = 1,
            ListName = "Quiz",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        dbContext.Topics.Add(new Topic
        {
            TopicId = 7,
            TopicName = "Actions",
            Status = UserStatus.Active,
        });

        AddWord(dbContext, 1, "run", "move quickly", "chay", now.AddMinutes(1), addToList: false);
        AddWord(dbContext, 2, "walk", "move on foot", "di bo", now.AddMinutes(2), addToList: false);
        AddWord(dbContext, 3, "jump", "push off the ground", "nhay", now.AddMinutes(3), addToList: false);
        AddWord(dbContext, 4, "fly", "move through air", "bay", now.AddMinutes(4), addToList: true);

        dbContext.TestSessions.Add(new TestSession
        {
            SessionId = 200,
            UserId = 1,
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
            StartedAt = now,
            Status = TestSessionStatus.InProgress,
        });

        await dbContext.SaveChangesAsync();
    }

    private static TestAnswer CreateAnswer(uint answerId, uint wordId, bool isCorrect)
    {
        return new TestAnswer
        {
            AnswerId = answerId,
            WordId = wordId,
            SenseId = wordId,
            QuestionNumber = (int)answerId,
            QuestionType = 1,
            DisplayContent = $"word-{wordId}",
            ExpectedAnswer = $"answer-{wordId}",
            UserAnswer = isCorrect ? $"answer-{wordId}" : "wrong",
            IsCorrect = isCorrect,
        };
    }

    private static void AddWord(
        VocaNovaDbContext dbContext,
        uint wordId,
        string wordText,
        string definition,
        string meaning,
        DateTime addedAt,
        bool addToList)
    {
        dbContext.Words.Add(new Word
        {
            WordId = wordId,
            Word1 = wordText,
            WordKey = wordText,
            Status = UserStatus.Active,
            CreatedAt = addedAt,
            UpdatedAt = addedAt,
            WordSenses =
            {
                new EntityWordSense
                {
                    SenseId = wordId,
                    WordId = wordId,
                    SenseOrder = 1,
                    WordClass = "verb",
                    EnglishDefinition = definition,
                    VietnameseMeaning = meaning,
                },
            },
            WordTopics =
            {
                new EntityWordTopic
                {
                    WordId = wordId,
                    TopicId = 7,
                },
            },
        });

        if (!addToList)
        {
            return;
        }

        dbContext.UserListWords.Add(new UserListWord
        {
            UserId = 1,
            ListId = 1,
            WordId = wordId,
            AddMethod = AddMethod.Manual,
            Status = UserStatus.Active,
            AddedAt = addedAt,
        });
    }
}
