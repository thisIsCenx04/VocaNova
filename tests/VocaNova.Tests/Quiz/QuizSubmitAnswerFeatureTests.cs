using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Features.Quiz.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Quiz;

public class QuizSubmitAnswerFeatureTests
{
    [Fact]
    public async Task SubmitAnswerAsync_Should_Grade_UpsertAnswer_UpdateSrs_And_Return_NextQuestion()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(dbContext, AnswerMethod.MultipleChoice);
        var service = CreateService(dbContext);

        var result = await service.SubmitAnswerAsync(
            1,
            100,
            new SubmitAnswerRequest(4, "bay"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCorrect.Should().BeTrue();
        result.Value.CorrectCount.Should().Be(1);
        result.Value.WrongCount.Should().Be(0);
        result.Value.Score.Should().Be(100);
        result.Value.NextQuestion.Should().NotBeNull();
        result.Value.NextQuestion!.WordId.Should().Be(3);

        var answer = await dbContext.TestAnswers.SingleAsync();
        answer.SessionId.Should().Be(100);
        answer.WordId.Should().Be(4);
        answer.UserAnswer.Should().Be("bay");
        answer.IsCorrect.Should().BeTrue();

        var session = await dbContext.TestSessions.SingleAsync(entity => entity.SessionId == 100);
        session.CorrectCount.Should().Be(1);
        session.WrongCount.Should().Be(0);
        session.MaxStreak.Should().Be(1);

        var progress = await dbContext.UserWordProgresses.SingleAsync();
        progress.UserId.Should().Be(1);
        progress.WordId.Should().Be(4);
        progress.CorrectCount.Should().Be(1);
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_Use_ExactTypingGrader_For_ExactTyping_Session()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(dbContext, AnswerMethod.ExactTyping);
        var service = CreateService(dbContext);

        var result = await service.SubmitAnswerAsync(
            1,
            100,
            new SubmitAnswerRequest(4, " BAY!!! "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_Use_AiGradingStub_For_AiTyping_Session()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(dbContext, AnswerMethod.AiTyping);
        var service = CreateService(dbContext);

        var result = await service.SubmitAnswerAsync(
            1,
            100,
            new SubmitAnswerRequest(4, "wrong"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCorrect.Should().BeFalse();
        result.Value.AiScore.Should().Be(0);
        result.Value.AiExplanation.Should().Be("Stub AI grading result.");

        var answer = await dbContext.TestAnswers.SingleAsync();
        answer.AiScore.Should().Be(0);
        answer.AiExplanation.Should().Be("Stub AI grading result.");
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_Return_409_When_Session_Not_InProgress()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(dbContext, AnswerMethod.MultipleChoice, TestSessionStatus.Completed);
        var service = CreateService(dbContext);

        var result = await service.SubmitAnswerAsync(
            1,
            100,
            new SubmitAnswerRequest(4, "bay"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Quiz session is not in progress.");
        (await dbContext.TestAnswers.CountAsync()).Should().Be(0);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static QuizSubmitService CreateService(VocaNovaDbContext dbContext)
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

    private static async Task SeedQuizDataAsync(
        VocaNovaDbContext dbContext,
        string answerMethod,
        string sessionStatus = TestSessionStatus.InProgress)
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

        AddWord(dbContext, 1, "run", "move quickly", "chay", now.AddMinutes(1));
        AddWord(dbContext, 2, "walk", "move on foot", "di bo", now.AddMinutes(2));
        AddWord(dbContext, 3, "jump", "push off the ground", "nhay", now.AddMinutes(3));
        AddWord(dbContext, 4, "fly", "move through air", "bay", now.AddMinutes(4));

        dbContext.TestSessions.Add(new TestSession
        {
            SessionId = 100,
            UserId = 1,
            TestType = answerMethod,
            Mode = TestMode.Standard,
            QuestionType = 1,
            ScopeType = ScopeType.All,
            WordOrder = WordOrder.Newest,
            QuestionCount = 4,
            CorrectCount = 0,
            WrongCount = 0,
            Score = 0,
            MaxStreak = 0,
            StartedAt = now,
            Status = sessionStatus,
        });

        await dbContext.SaveChangesAsync();
    }

    private static void AddWord(
        VocaNovaDbContext dbContext,
        uint wordId,
        string wordText,
        string definition,
        string meaning,
        DateTime addedAt)
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
                new WordSense
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
                new WordTopic
                {
                    WordId = wordId,
                    TopicId = 7,
                },
            },
        });

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
