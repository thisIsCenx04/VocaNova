using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Features.Quiz.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Quiz;

public class QuizWordPoolBuilderTests
{
    [Fact]
    public async Task BuildPoolAsync_Should_Return_All_Active_User_List_Words()
    {
        await using var dbContext = CreateDbContext();
        await SeedPoolWordsAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildPoolAsync(
            1,
            CreateRequest(scopeType: ScopeType.All, answerMethod: AnswerMethod.ExactTyping));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Select(word => word.WordId).Should().Equal(4u, 3u, 2u, 1u);
    }

    [Fact]
    public async Task BuildPoolAsync_Should_Filter_By_DateRange_Inclusively()
    {
        await using var dbContext = CreateDbContext();
        await SeedPoolWordsAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildPoolAsync(
            1,
            CreateRequest(
                scopeType: ScopeType.DateRange,
                from: new DateOnly(2026, 1, 2),
                to: new DateOnly(2026, 1, 3),
                answerMethod: AnswerMethod.ExactTyping));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(word => word.WordId).Should().Equal(3u, 2u);
    }

    [Fact]
    public async Task BuildPoolAsync_Should_Filter_By_Topics_Apply_Order_And_Limit()
    {
        await using var dbContext = CreateDbContext();
        await SeedPoolWordsAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildPoolAsync(
            1,
            CreateRequest(
                topicIds: new[] { 7u },
                wordOrder: WordOrder.Oldest,
                wordLimit: 2,
                answerMethod: AnswerMethod.ExactTyping));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(word => word.WordId).Should().Equal(1u, 3u);
    }

    [Fact]
    public async Task BuildPoolAsync_Should_Return_400_When_MultipleChoice_Pool_Has_Less_Than_Four_Words()
    {
        await using var dbContext = CreateDbContext();
        await SeedSmallPoolAsync(dbContext);
        var builder = CreateBuilder(dbContext);

        var result = await builder.BuildPoolAsync(
            1,
            CreateRequest(scopeType: ScopeType.All, answerMethod: AnswerMethod.MultipleChoice));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Không đủ từ để tạo bài kiểm tra");
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static QuizSessionBuilder CreateBuilder(VocaNovaDbContext dbContext)
    {
        return new QuizSessionBuilder(new QuizWordPoolRepository(dbContext));
    }

    private static BuildQuizPoolRequest CreateRequest(
        string scopeType = ScopeType.All,
        DateOnly? from = null,
        DateOnly? to = null,
        IReadOnlyCollection<uint>? topicIds = null,
        string wordOrder = WordOrder.Newest,
        int? wordLimit = null,
        string answerMethod = AnswerMethod.ExactTyping)
    {
        return new BuildQuizPoolRequest(
            scopeType,
            from,
            to,
            topicIds,
            wordOrder,
            wordLimit,
            answerMethod);
    }

    private static async Task SeedPoolWordsAsync(VocaNovaDbContext dbContext)
    {
        var baseDate = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        dbContext.UserLists.AddRange(
            new UserList
            {
                ListId = 1,
                UserId = 1,
                ListName = "Active",
                Status = UserStatus.Active,
                CreatedAt = baseDate,
            },
            new UserList
            {
                ListId = 2,
                UserId = 1,
                ListName = "Deleted",
                Status = UserStatus.Deleted,
                CreatedAt = baseDate,
            },
            new UserList
            {
                ListId = 3,
                UserId = 2,
                ListName = "Other User",
                Status = UserStatus.Active,
                CreatedAt = baseDate,
            });

        dbContext.Topics.AddRange(
            new Topic
            {
                TopicId = 7,
                TopicName = "Travel",
                Status = UserStatus.Active,
            },
            new Topic
            {
                TopicId = 8,
                TopicName = "Food",
                Status = UserStatus.Active,
            });

        for (var index = 1; index <= 6; index++)
        {
            dbContext.Words.Add(new Word
            {
                WordId = (uint)index,
                Word1 = $"word-{index}",
                WordKey = $"word-{index}",
                Status = index == 6 ? UserStatus.Deleted : UserStatus.Active,
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
            });
        }

        dbContext.WordTopics.AddRange(
            new WordTopic { WordId = 1, TopicId = 7 },
            new WordTopic { WordId = 2, TopicId = 8 },
            new WordTopic { WordId = 3, TopicId = 7 },
            new WordTopic { WordId = 4, TopicId = 8 },
            new WordTopic { WordId = 5, TopicId = 7 },
            new WordTopic { WordId = 6, TopicId = 7 });

        dbContext.UserListWords.AddRange(
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 1,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = baseDate,
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 2,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = baseDate.AddDays(1),
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 3,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = baseDate.AddDays(2),
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 4,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = baseDate.AddDays(3),
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 5,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Deleted,
                AddedAt = baseDate.AddDays(4),
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 2,
                WordId = 5,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = baseDate.AddDays(4),
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 6,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = baseDate.AddDays(5),
            },
            new UserListWord
            {
                UserId = 2,
                ListId = 3,
                WordId = 5,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = baseDate.AddDays(6),
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSmallPoolAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 1,
            UserId = 1,
            ListName = "Small",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        for (var index = 1; index <= 3; index++)
        {
            dbContext.Words.Add(new Word
            {
                WordId = (uint)index,
                Word1 = $"word-{index}",
                WordKey = $"word-{index}",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            });

            dbContext.UserListWords.Add(new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = (uint)index,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now.AddMinutes(index),
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
