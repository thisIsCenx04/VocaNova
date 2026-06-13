using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Features.Quiz.Services;
using VocaNova.API.Features.Quiz.Validators;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Quiz;

public class QuizCreateSessionFeatureTests
{
    [Fact]
    public async Task CreateSessionAsync_Should_Create_InProgress_Session_With_Topics_And_FirstQuestion()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizSessionDataAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateSessionAsync(
            1,
            CreateRequest(topicIds: new[] { 7u }));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Session.Status.Should().Be(TestSessionStatus.InProgress);
        result.Value.Session.AnswerMethod.Should().Be(AnswerMethod.MultipleChoice);
        result.Value.Session.Mode.Should().Be(TestMode.Standard);
        result.Value.Session.QuestionCount.Should().Be(4);
        result.Value.Session.TopicIds.Should().BeEquivalentTo(new[] { 7u });
        result.Value.FirstQuestion.WordId.Should().Be(4);
        result.Value.FirstQuestion.Choices.Should().HaveCount(4);

        var session = await dbContext.TestSessions
            .Include(entity => entity.TestSessionTopics)
            .SingleAsync();
        session.Status.Should().Be(TestSessionStatus.InProgress);
        session.TestType.Should().Be(AnswerMethod.MultipleChoice);
        session.QuestionCount.Should().Be(4);
        session.TestSessionTopics.Should().ContainSingle()
            .Which.TopicId.Should().Be(7);
    }

    [Fact]
    public void CreateSessionRequestValidator_Should_Reject_Timed_Mode_Without_TimeLimit()
    {
        var validator = new CreateSessionRequestValidator();

        var result = validator.Validate(CreateRequest(mode: TestMode.Timed, timeLimitSec: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateSessionRequest.TimeLimitSec));
    }

    [Fact]
    public void CreateSessionRequestValidator_Should_Reject_Elimination_Mode_Without_Lives()
    {
        var validator = new CreateSessionRequestValidator();

        var result = validator.Validate(CreateRequest(mode: TestMode.Elimination, lives: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateSessionRequest.Lives));
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Return_400_When_Timed_Mode_Misses_TimeLimit()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizSessionDataAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateSessionAsync(
            1,
            CreateRequest(mode: TestMode.Timed, timeLimitSec: null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Timed mode requires time_limit_sec.");
        (await dbContext.TestSessions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Return_400_When_Elimination_Mode_Misses_Lives()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizSessionDataAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateSessionAsync(
            1,
            CreateRequest(mode: TestMode.Elimination, lives: null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Elimination mode requires lives.");
        (await dbContext.TestSessions.CountAsync()).Should().Be(0);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static QuizSessionService CreateService(VocaNovaDbContext dbContext)
    {
        return new QuizSessionService(
            new QuizSessionBuilder(new QuizWordPoolRepository(dbContext)),
            new QuizQuestionBuilder(new QuizQuestionRepository(dbContext)),
            new QuizSessionRepository(dbContext));
    }

    private static CreateSessionRequest CreateRequest(
        string mode = TestMode.Standard,
        int questionType = 1,
        string scopeType = ScopeType.All,
        DateOnly? from = null,
        DateOnly? to = null,
        IReadOnlyCollection<uint>? topicIds = null,
        string wordOrder = WordOrder.Newest,
        int? wordLimit = null,
        int? timeLimitSec = null,
        int? lives = null,
        string answerMethod = AnswerMethod.MultipleChoice)
    {
        return new CreateSessionRequest(
            mode,
            questionType,
            scopeType,
            from,
            to,
            topicIds,
            wordOrder,
            wordLimit,
            timeLimitSec,
            lives,
            answerMethod);
    }

    private static async Task SeedQuizSessionDataAsync(VocaNovaDbContext dbContext)
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
