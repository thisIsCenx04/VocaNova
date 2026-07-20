using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Repositories;
using VocaNova.API.Features.Lists.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Lists;

public sealed class PersonalTopicFeatureTests
{
    [Fact]
    public async Task GetTopicsAsync_Should_Keep_Global_Topics_And_Return_User_Counts()
    {
        await using var dbContext = CreateDbContext();
        await SeedDictionaryAsync(dbContext);
        await SeedPersonalTopicAsync(dbContext, userId: 1, topicId: 1, listId: 10, wordIds: [1]);
        await SeedRegularListAsync(dbContext, userId: 1, listId: 20, wordId: 2);
        var service = CreateService(dbContext);

        var result = await service.GetTopicsAsync(userId: 1, wordId: 2);

        result.IsSuccess.Should().BeTrue();
        var topics = result.Value!;
        topics.Should().HaveCount(3);
        var fruit = topics.Single(topic => topic.TopicId == 1);
        fruit.Name.Should().Be("Fruit");
        fruit.ListId.Should().Be(10);
        fruit.WordCount.Should().Be(1);
        fruit.ContainsWord.Should().BeFalse();
        var color = topics.Single(topic => topic.TopicId == 3);
        color.Name.Should().Be("Color");
        color.ListId.Should().BeNull();
        color.WordCount.Should().Be(0);
        color.ContainsWord.Should().BeFalse();
    }

    [Fact]
    public async Task AddWordAsync_Should_Create_Internal_List_Without_Changing_Schema_Or_Normal_Lists()
    {
        await using var dbContext = CreateDbContext();
        await SeedDictionaryAsync(dbContext);
        var personalService = CreateService(dbContext);
        var userListService = new UserListService(new UserListRepository(dbContext));

        var result = await personalService.AddWordAsync(
            userId: 1,
            topicId: 1,
            new AddPersonalTopicWordRequest(1, " first fruit "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordCount.Should().Be(1);
        result.Value.ContainsWord.Should().BeTrue();
        result.Value.ListId.Should().NotBeNull();

        var internalList = await dbContext.UserLists.SingleAsync();
        internalList.ListName.Should().Be(PersonalTopicListName.For(1));
        var savedWord = await dbContext.UserListWords.SingleAsync();
        savedWord.Note.Should().Be("first fruit");
        savedWord.AddMethod.Should().Be(AddMethod.Search);

        var normalLists = await userListService.GetByUserAsync(1);
        normalLists.IsSuccess.Should().BeTrue();
        normalLists.Value.Should().BeEmpty();

        dbContext.Model.GetEntityTypes()
            .Should().NotContain(entity => entity.GetTableName() == "user_topic_words");
    }

    [Fact]
    public async Task AddWordAsync_Should_Allow_Same_Word_In_Two_Eligible_Personal_Topics()
    {
        await using var dbContext = CreateDbContext();
        await SeedDictionaryAsync(dbContext);
        var service = CreateService(dbContext);

        var fruit = await service.AddWordAsync(1, 1, new AddPersonalTopicWordRequest(1, null));
        var tree = await service.AddWordAsync(1, 2, new AddPersonalTopicWordRequest(1, null));

        fruit.IsSuccess.Should().BeTrue();
        tree.IsSuccess.Should().BeTrue();
        (await dbContext.UserLists.CountAsync()).Should().Be(2);
        (await dbContext.UserListWords.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task AddWordAsync_Should_Reject_Topic_Not_Assigned_By_Admin()
    {
        await using var dbContext = CreateDbContext();
        await SeedDictionaryAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.AddWordAsync(
            userId: 1,
            topicId: 2,
            new AddPersonalTopicWordRequest(2, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Word does not belong to this system topic.");
        dbContext.UserLists.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWordsAsync_Should_Return_Only_Selected_Personal_Topic_List()
    {
        await using var dbContext = CreateDbContext();
        await SeedDictionaryAsync(dbContext);
        await SeedPersonalTopicAsync(dbContext, 1, 1, 10, [1]);
        await SeedRegularListAsync(dbContext, 1, 20, 2);
        var service = CreateService(dbContext);

        var result = await service.GetWordsAsync(
            1,
            1,
            new ListWordsQuery { Page = 1, Limit = 20 });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle().Which.WordId.Should().Be(1);
    }

    [Fact]
    public async Task Normal_List_Endpoints_Should_Not_Manage_Internal_Topic_Lists()
    {
        await using var dbContext = CreateDbContext();
        await SeedDictionaryAsync(dbContext);
        await SeedPersonalTopicAsync(dbContext, 1, 1, 10, [1]);
        var service = new UserListService(new UserListRepository(dbContext));

        var getWords = await service.GetWordsAsync(
            1,
            10,
            new ListWordsQuery { Page = 1, Limit = 20 });
        var rename = await service.UpdateAsync(1, 10, new UpdateListRequest("Renamed"));
        var delete = await service.SoftDeleteAsync(1, 10);

        getWords.StatusCode.Should().Be(404);
        rename.StatusCode.Should().Be(404);
        delete.StatusCode.Should().Be(404);
    }

    private static PersonalTopicService CreateService(VocaNovaDbContext dbContext)
    {
        var userListRepository = new UserListRepository(dbContext);
        return new PersonalTopicService(
            new PersonalTopicRepository(dbContext),
            userListRepository);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VocaNovaDbContext(options);
    }

    private static async Task SeedDictionaryAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.Topics.AddRange(
            new Topic { TopicId = 1, TopicName = "Fruit", Status = UserStatus.Active },
            new Topic { TopicId = 2, TopicName = "Tree", Status = UserStatus.Active },
            new Topic { TopicId = 3, TopicName = "Color", Status = UserStatus.Active });
        dbContext.Words.AddRange(
            new Word
            {
                WordId = 1,
                Word1 = "apple",
                WordKey = "apple",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                WordSenses =
                {
                    new WordSense
                    {
                        SenseId = 1,
                        WordId = 1,
                        SenseOrder = 1,
                        WordClass = "noun",
                        EnglishDefinition = "a fruit",
                        VietnameseMeaning = "tao",
                    },
                },
            },
            new Word
            {
                WordId = 2,
                Word1 = "orange",
                WordKey = "orange",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            });
        dbContext.WordTopics.AddRange(
            new WordTopic { WordId = 1, TopicId = 1 },
            new WordTopic { WordId = 1, TopicId = 2 },
            new WordTopic { WordId = 2, TopicId = 1 },
            new WordTopic { WordId = 2, TopicId = 3 });
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPersonalTopicAsync(
        VocaNovaDbContext dbContext,
        uint userId,
        uint topicId,
        uint listId,
        IReadOnlyCollection<uint> wordIds)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = listId,
            UserId = userId,
            ListName = PersonalTopicListName.For(topicId),
            Status = UserStatus.Active,
            CreatedAt = now,
        });
        foreach (var wordId in wordIds)
        {
            dbContext.UserListWords.Add(new UserListWord
            {
                UserId = userId,
                ListId = listId,
                WordId = wordId,
                AddMethod = AddMethod.Search,
                Status = UserStatus.Active,
                AddedAt = now,
            });
        }
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRegularListAsync(
        VocaNovaDbContext dbContext,
        uint userId,
        uint listId,
        uint wordId)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = listId,
            UserId = userId,
            ListName = "My list",
            Status = UserStatus.Active,
            CreatedAt = now,
        });
        dbContext.UserListWords.Add(new UserListWord
        {
            UserId = userId,
            ListId = listId,
            WordId = wordId,
            AddMethod = AddMethod.Search,
            Status = UserStatus.Active,
            AddedAt = now,
        });
        await dbContext.SaveChangesAsync();
    }
}
