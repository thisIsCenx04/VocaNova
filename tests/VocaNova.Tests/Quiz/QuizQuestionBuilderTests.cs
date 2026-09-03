using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.DAL.Repositories;
using VocaNova.API.Features.Quiz.BLL.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Quiz;

public class QuizQuestionBuilderTests
{
    [Fact]
    public async Task BuildQuestionAsync_Should_Build_WordToMeaning_Question_With_Four_Unique_Choices()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuestionWordsAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildQuestionAsync(1, questionType: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.WordId.Should().Be(1);
        result.Value.SenseId.Should().Be(1);
        result.Value.QuestionType.Should().Be(1);
        result.Value.DisplayContent.Should().Be("run");
        result.Value.ExpectedAnswer.Should().Be("chay");
        result.Value.Choices.Should().HaveCount(4);
        result.Value.Choices.Should().Contain("chay");
        result.Value.Choices.Distinct(StringComparer.OrdinalIgnoreCase).Should().HaveCount(4);
    }

    [Fact]
    public async Task BuildQuestionAsync_Should_Build_MeaningToWord_Question()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuestionWordsAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildQuestionAsync(1, questionType: 2);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayContent.Should().Be("chay");
        result.Value.ExpectedAnswer.Should().Be("run");
        result.Value.Choices.Should().HaveCount(4);
        result.Value.Choices.Should().Contain("run");
        result.Value.Choices.Distinct(StringComparer.OrdinalIgnoreCase).Should().HaveCount(4);
    }

    [Fact]
    public async Task BuildQuestionAsync_Should_Build_Description_Question()
    {
        await using var dbContext = CreateDbContext();
        await SeedQuestionWordsAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildQuestionAsync(1, questionType: 3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayContent.Should().Be("move quickly on foot");
        result.Value.ExpectedAnswer.Should().Be("run");
        result.Value.Choices.Should().HaveCount(4);
        result.Value.Choices.Should().Contain("run");
    }

    [Fact]
    public async Task BuildQuestionAsync_Should_Return_400_When_Distractors_Are_Not_Enough()
    {
        await using var dbContext = CreateDbContext();
        await SeedInsufficientDistractorsAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildQuestionAsync(1, questionType: 1);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Không đủ đáp án gây nhiễu để tạo câu hỏi");
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static QuizQuestionBuilder CreateBuilder(VocaNovaDbContext dbContext)
    {
        return new QuizQuestionBuilder(new QuizQuestionRepository(dbContext));
    }

    private static async Task SeedQuestionWordsAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.Topics.AddRange(
            new Topic
            {
                TopicId = 7,
                TopicName = "Actions",
                Status = UserStatus.Active,
            },
            new Topic
            {
                TopicId = 8,
                TopicName = "Movement",
                Status = UserStatus.Active,
            });

        AddWord(dbContext, 1, "run", "verb", "move quickly on foot", "chay", now, 7);
        AddWord(dbContext, 2, "walk", "verb", "move at a regular pace", "di bo", now, 7);
        AddWord(dbContext, 3, "sprint", "verb", "run very fast", "chay", now, 7);
        AddWord(dbContext, 4, "jump", "verb", "push yourself off the ground", "nhay", now, 8);
        AddWord(dbContext, 5, "fly", "verb", "move through air", "bay", now, 7);
        AddWord(dbContext, 6, "bright", "adjective", "full of light", "sang", now, 7);
        AddWord(dbContext, 7, "deleted", "verb", "deleted word", "da xoa", now, 7, UserStatus.Deleted);

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedInsufficientDistractorsAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.Topics.Add(new Topic
        {
            TopicId = 7,
            TopicName = "Actions",
            Status = UserStatus.Active,
        });

        AddWord(dbContext, 1, "run", "verb", "move quickly on foot", "chay", now, 7);
        AddWord(dbContext, 2, "walk", "verb", "move at a regular pace", "di bo", now, 7);
        AddWord(dbContext, 3, "sprint", "verb", "run very fast", "chay", now, 7);

        await dbContext.SaveChangesAsync();
    }

    private static void AddWord(
        VocaNovaDbContext dbContext,
        uint wordId,
        string wordText,
        string wordClass,
        string englishDefinition,
        string vietnameseMeaning,
        DateTime now,
        uint topicId,
        string status = UserStatus.Active)
    {
        dbContext.Words.Add(new Word
        {
            WordId = wordId,
            Word1 = wordText,
            WordKey = wordText,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
            WordSenses =
            {
                new EntityWordSense
                {
                    SenseId = wordId,
                    WordId = wordId,
                    SenseOrder = 1,
                    WordClass = wordClass,
                    EnglishDefinition = englishDefinition,
                    VietnameseMeaning = vietnameseMeaning,
                },
            },
            WordTopics =
            {
                new EntityWordTopic
                {
                    WordId = wordId,
                    TopicId = topicId,
                },
            },
        });
    }
}
