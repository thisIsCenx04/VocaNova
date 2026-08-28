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
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Infrastructure.Caching.Dictionary;
using VocaNova.API.Infrastructure.Caching.Knn;
using VocaNova.API.Infrastructure.Caching.Lists;
using VocaNova.API.Infrastructure.Caching.Progress;
using VocaNova.API.Infrastructure.Caching.Quiz;
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
            new SubmitAnswerRequest(4, "bay").ToBusinessCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCorrect.Should().BeTrue();
        result.Value.CorrectCount.Should().Be(1);
        result.Value.WrongCount.Should().Be(0);
        // Score is over the whole quiz: 1 correct of 4 questions = 25%.
        result.Value.Score.Should().Be(25);
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
            new SubmitAnswerRequest(4, " BAY!!! ").ToBusinessCommand());

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
            new SubmitAnswerRequest(4, "wrong").ToBusinessCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCorrect.Should().BeFalse();
        result.Value.AiScore.Should().Be(0);
        result.Value.AiExplanation.Should().Be("Stub AI grading result.");

        var answer = await dbContext.TestAnswers.SingleAsync();
        answer.AiScore.Should().Be(0);
        answer.AiExplanation.Should().Be("Stub AI grading result.");
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_Accept_Candidate_Even_When_WordLimit_Below_Pool()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(
            dbContext,
            AnswerMethod.MultipleChoice,
            wordOrder: WordOrder.Random,
            wordLimit: 2,
            questionCount: 2);
        var service = CreateService(dbContext);

        // Word 4 is a valid candidate. With the old behaviour the submit pool was
        // re-shuffled and truncated to word_limit (2 of 4), which could exclude
        // word 4 and reject the answer. Validation now uses the full candidate set.
        var result = await service.SubmitAnswerAsync(
            1,
            100,
            new SubmitAnswerRequest(4, "bay").ToBusinessCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_Complete_After_QuestionCount_Answers()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(
            dbContext,
            AnswerMethod.ExactTyping,
            wordOrder: WordOrder.Random,
            wordLimit: 2,
            questionCount: 2);
        var service = CreateService(dbContext);

        var first = await service.SubmitAnswerAsync(
            1,
            100,
            new SubmitAnswerRequest(4, "bay").ToBusinessCommand());
        first.IsSuccess.Should().BeTrue();
        first.Value!.NextQuestion.Should().NotBeNull();

        var second = await service.SubmitAnswerAsync(
            1,
            100,
            new SubmitAnswerRequest(first.Value.NextQuestion!.WordId, "anything").ToBusinessCommand());
        second.IsSuccess.Should().BeTrue();
        // Two answers reach the session's question_count, so the quiz ends here.
        second.Value!.NextQuestion.Should().BeNull();

        var session = await dbContext.TestSessions.SingleAsync(entity => entity.SessionId == 100);
        session.Status.Should().Be(TestSessionStatus.Completed);
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
            new SubmitAnswerRequest(4, "bay").ToBusinessCommand());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Quiz session is not in progress.");
        (await dbContext.TestAnswers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_Build_Word_Pool_Once_And_Reuse_It_From_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(dbContext, AnswerMethod.MultipleChoice);
        var cache = new FakeQuizPoolCache();
        var service = CreateService(dbContext, cache);

        var first = await service.SubmitAnswerAsync(1, 100, new SubmitAnswerRequest(4, "bay").ToBusinessCommand());
        first.IsSuccess.Should().BeTrue();
        cache.SetCount.Should().Be(1, "tập từ được dựng và cache ở lần nộp đầu");
        cache.HitCount.Should().Be(0);

        var second = await service.SubmitAnswerAsync(1, 100, new SubmitAnswerRequest(3, "nhay").ToBusinessCommand());

        second.IsSuccess.Should().BeTrue();
        cache.HitCount.Should().Be(1, "lần nộp sau đọc lại từ cache");
        cache.SetCount.Should().Be(1, "không dựng lại tập từ");
    }

    [Fact]
    public async Task SubmitAnswerAsync_Should_Drop_Cached_Pool_When_Session_Completes()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuizDataAsync(dbContext, AnswerMethod.MultipleChoice, questionCount: 1);
        var cache = new FakeQuizPoolCache();
        var service = CreateService(dbContext, cache);

        var result = await service.SubmitAnswerAsync(1, 100, new SubmitAnswerRequest(4, "bay").ToBusinessCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.NextQuestion.Should().BeNull("phiên chỉ có một câu");
        cache.RemoveCount.Should().Be(1, "phiên kết thúc thì bỏ cache");
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static QuizSubmitService CreateService(
        VocaNovaDbContext dbContext,
        IQuizPoolCache? quizPoolCache = null)
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
            new SrsService(new SrsRepository(dbContext)),
            progressCache: null,
            poolCache: quizPoolCache);
    }

    /// <summary>
    /// Cache trong bộ nhớ, đếm số lần đọc/ghi để khẳng định tập từ chỉ được
    /// dựng một lần cho mỗi phiên.
    /// </summary>
    private sealed class FakeQuizPoolCache : IQuizPoolCache
    {
        private readonly Dictionary<string, IReadOnlyCollection<QuizPoolWordDto>> _entries = new();

        public int SetCount { get; private set; }

        public int HitCount { get; private set; }

        public int RemoveCount { get; private set; }

        public Task<IReadOnlyCollection<QuizPoolWordDto>?> GetAsync(
            uint sessionId,
            uint? listId,
            CancellationToken cancellationToken = default)
        {
            if (_entries.TryGetValue(Key(sessionId, listId), out var pool))
            {
                HitCount++;
                return Task.FromResult<IReadOnlyCollection<QuizPoolWordDto>?>(pool);
            }

            return Task.FromResult<IReadOnlyCollection<QuizPoolWordDto>?>(null);
        }

        public Task SetAsync(
            uint sessionId,
            uint? listId,
            IReadOnlyCollection<QuizPoolWordDto> pool,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            _entries[Key(sessionId, listId)] = pool;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            uint sessionId,
            uint? listId,
            CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            _entries.Remove(Key(sessionId, listId));
            return Task.CompletedTask;
        }

        private static string Key(uint sessionId, uint? listId)
        {
            return $"{sessionId}:{listId?.ToString() ?? "all"}";
        }
    }

    private static async Task SeedQuizDataAsync(
        VocaNovaDbContext dbContext,
        string answerMethod,
        string sessionStatus = TestSessionStatus.InProgress,
        string wordOrder = WordOrder.Newest,
        int? wordLimit = null,
        int questionCount = 4)
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
            WordOrder = wordOrder,
            WordLimit = wordLimit,
            QuestionCount = questionCount,
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
