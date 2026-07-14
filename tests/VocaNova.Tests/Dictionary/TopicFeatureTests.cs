using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.Validators;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Dictionary;

public class TopicFeatureTests
{
    [Fact]
    public async Task GetTopicsAsync_Should_Return_Active_Topics_With_WordCount()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetTopicsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var topics = result.Value!;
        topics.Select(topic => topic.Name).Should().Equal("Empty Topic", "Movement", "Sports");
        topics.Single(topic => topic.TopicId == 1).WordCount.Should().Be(2);
        topics.Should().NotContain(topic => topic.TopicId == 3);
    }

    [Fact]
    public async Task GetAdminTopicsAsync_Should_Return_Active_With_ActiveWordCount_By_Default()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetAdminTopicsAsync(new AdminTopicQuery());

        result.IsSuccess.Should().BeTrue();
        var topics = result.Value!;
        topics.Should().NotContain(topic => topic.TopicId == 3); // deleted hidden by default
        topics.Single(topic => topic.TopicId == 1).WordCount.Should().Be(2);
        topics.Single(topic => topic.TopicId == 2).WordCount.Should().Be(1);
        topics.Single(topic => topic.TopicId == 4).WordCount.Should().Be(0);
        topics.Single(topic => topic.TopicId == 1).Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task GetAdminTopicsAsync_Should_Include_Deleted_And_Filter_By_Search()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var withDeleted = await service.GetAdminTopicsAsync(new AdminTopicQuery { IncludeDeleted = true });
        withDeleted.Value!.Should().Contain(topic => topic.TopicId == 3 && topic.Status == UserStatus.Deleted);

        var searched = await service.GetAdminTopicsAsync(new AdminTopicQuery { Q = "sport" });
        searched.Value!.Select(topic => topic.TopicId).Should().Equal(2u);
    }

    [Fact]
    public async Task GetAdminTopicsAsync_Should_Reject_Invalid_Status()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetAdminTopicsAsync(new AdminTopicQuery { Status = "archived" });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetWordsAsync_Should_Return_Paginated_Words_By_Topic()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetWordsAsync(1, new TopicWordsQuery { Page = 1, Limit = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalItems.Should().Be(2);
        result.Value.Items.Should().ContainSingle()
            .Which.Word.Should().Be("run");
    }

    [Fact]
    public async Task GetWordsAsync_Should_Return_404_When_Topic_Is_Not_Active()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetWordsAsync(3, new TopicWordsQuery());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Be("Topic not found.");
    }

    [Fact]
    public async Task GetTopicsAsync_Should_Return_Cached_Topics_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cachedTopics = new[]
        {
            new TopicSummaryDto(99, "Cached", null, null, 7),
        };
        var cache = new FakeTopicCache(cachedTopics: cachedTopics);
        var service = CreateService(dbContext, cache);

        var result = await service.GetTopicsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedTopics);
        cache.GetTopicsCount.Should().Be(1);
        cache.SetTopicsCount.Should().Be(0);
        (await dbContext.Topics.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetWordsAsync_Should_Return_Cached_TopicWords_When_Available()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var cachedWords = new PagedResult<WordSummaryDto>(
            new[] { new WordSummaryDto(99, "cached", null, null, null, null) },
            1,
            20,
            1);
        var cache = new FakeTopicCache(cachedTopicWords: cachedWords);
        var service = CreateService(dbContext, cache);

        var result = await service.GetWordsAsync(1, new TopicWordsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedWords);
        cache.GetTopicWordsCount.Should().Be(1);
        cache.SetTopicWordsCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Topic_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        var cache = new FakeTopicCache();
        var service = CreateService(dbContext, cache);

        var result = await service.CreateAsync(new CreateTopicRequest("Travel", "Du lich", "plane"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Travel");
        result.Value.NameVi.Should().Be("Du lich");
        result.Value.Icon.Should().Be("plane");
        result.Value.WordCount.Should().Be(0);
        cache.RemoveTopicsCount.Should().Be(1);

        var topic = await dbContext.Topics.SingleAsync();
        topic.TopicName.Should().Be("Travel");
        topic.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task CreateAsync_Should_Insert_Selected_Words_Into_WordTopics()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateTopicRequest(
            "Food", "Thuc an", null, new uint[] { 1, 2 }));

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordCount.Should().Be(2);
        var links = await dbContext.WordTopics
            .Where(link => link.TopicId == result.Value.TopicId)
            .ToListAsync();
        links.Select(link => link.WordId).Should().BeEquivalentTo(new uint[] { 1, 2 });
        links.Should().OnlyContain(link => link.IsPrimary);
    }

    [Fact]
    public async Task UpdateAsync_Should_Replace_WordTopics_With_Selected_Words()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(
            1, new UpdateTopicRequest("Movement", "Van dong", "run", new uint[] { 3 }));

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordCount.Should().Be(1);
        var links = await dbContext.WordTopics.Where(link => link.TopicId == 1).ToListAsync();
        links.Should().ContainSingle();
        links[0].WordId.Should().Be(3);
        links[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_Should_Restore_Deleted_Topic_And_Add_Selected_Words()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateTopicRequest(
            "Deleted Topic", "Chu de khoi phuc", "restore", new uint[] { 2, 3 }));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TopicId.Should().Be(3);
        result.Value.WordCount.Should().Be(2);

        var restored = await dbContext.Topics
            .Include(topic => topic.WordTopics)
            .SingleAsync(topic => topic.TopicId == 3);
        restored.Status.Should().Be(UserStatus.Active);
        restored.TopicNameVi.Should().Be("Chu de khoi phuc");
        restored.WordTopics.Select(link => link.WordId).Should().BeEquivalentTo(new uint[] { 2, 3 });
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_Active_Topic()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateTopicRequest("Sports", null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Topic already exists.");
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_Vietnamese_Name_Case_Insensitively()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateTopicRequest("Athletics", "  THE THAO  ", null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Vietnamese topic name already exists.");
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Duplicate_Vietnamese_Name()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(
            1, new UpdateTopicRequest("Movement", "The thao", "run"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Vietnamese topic name already exists.");
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Delete_Topic_And_Keep_Word_Links()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SoftDeleteAsync(1);

        result.IsSuccess.Should().BeTrue();

        var topic = await dbContext.Topics.IgnoreQueryFilters().SingleAsync(entity => entity.TopicId == 1);
        topic.Status.Should().Be(UserStatus.Deleted);
        (await dbContext.WordTopics.CountAsync(link => link.TopicId == 1)).Should().Be(2);
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Delete_Topic_When_It_Has_No_Active_Words()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var cache = new FakeTopicCache();
        var service = CreateService(dbContext, cache);

        var result = await service.SoftDeleteAsync(4);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        cache.RemoveTopicsCount.Should().Be(1);

        var topic = await dbContext.Topics
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.TopicId == 4);
        topic.Status.Should().Be(UserStatus.Deleted);
        (await dbContext.Topics.AnyAsync(entity => entity.TopicId == 4)).Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAsync_Should_Restore_Deleted_Topic_Using_IgnoreQueryFilters()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var cache = new FakeTopicCache();
        var service = CreateService(dbContext, cache);

        var result = await service.RestoreAsync(3);

        result.IsSuccess.Should().BeTrue();
        cache.RemoveTopicsCount.Should().Be(1);

        var topic = await dbContext.Topics.SingleAsync(entity => entity.TopicId == 3);
        topic.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void CreateTopicRequestValidator_Should_Reject_Empty_Name()
    {
        var validator = new CreateTopicRequestValidator();

        var result = validator.Validate(new CreateTopicRequest("", null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateTopicRequest.TopicName));
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static TopicService CreateService(
        VocaNovaDbContext dbContext,
        ITopicCache? topicCache = null)
    {
        return new TopicService(
            new TopicRepository(dbContext),
            new WordRepository(dbContext),
            topicCache);
    }

    private static async Task SeedTopicsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Topics.AddRange(
            new Topic
            {
                TopicId = 1,
                TopicName = "Movement",
                TopicNameVi = "Van dong",
                Icon = "run",
                Status = UserStatus.Active,
            },
            new Topic
            {
                TopicId = 2,
                TopicName = "Sports",
                TopicNameVi = "The thao",
                Icon = "ball",
                Status = UserStatus.Active,
            },
            new Topic
            {
                TopicId = 3,
                TopicName = "Deleted Topic",
                Status = UserStatus.Deleted,
            },
            new Topic
            {
                TopicId = 4,
                TopicName = "Empty Topic",
                Status = UserStatus.Active,
            });

        dbContext.Words.AddRange(
            new Word
            {
                WordId = 1,
                Word1 = "run",
                WordKey = "run",
                CefrLevel = CefrLevel.A1,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                WordSenses =
                {
                    new WordSense
                    {
                        SenseId = 1,
                        WordId = 1,
                        SenseOrder = 1,
                        WordClass = "verb",
                        EnglishDefinition = "move quickly",
                        VietnameseMeaning = "chay",
                    },
                },
            },
            new Word
            {
                WordId = 2,
                Word1 = "walk",
                WordKey = "walk",
                CefrLevel = CefrLevel.A1,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Word
            {
                WordId = 3,
                Word1 = "football",
                WordKey = "football",
                CefrLevel = CefrLevel.A2,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        dbContext.WordTopics.AddRange(
            new WordTopic
            {
                WordId = 1,
                TopicId = 1,
            },
            new WordTopic
            {
                WordId = 2,
                TopicId = 1,
            },
            new WordTopic
            {
                WordId = 3,
                TopicId = 2,
            });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeTopicCache : ITopicCache
    {
        private readonly IReadOnlyCollection<TopicSummaryDto>? _cachedTopics;
        private readonly PagedResult<WordSummaryDto>? _cachedTopicWords;

        public FakeTopicCache(
            IReadOnlyCollection<TopicSummaryDto>? cachedTopics = null,
            PagedResult<WordSummaryDto>? cachedTopicWords = null)
        {
            _cachedTopics = cachedTopics;
            _cachedTopicWords = cachedTopicWords;
        }

        public int GetTopicsCount { get; private set; }

        public int SetTopicsCount { get; private set; }

        public int GetTopicWordsCount { get; private set; }

        public int SetTopicWordsCount { get; private set; }

        public int RemoveTopicsCount { get; private set; }

        public Task<IReadOnlyCollection<TopicSummaryDto>?> GetTopicsAsync(
            CancellationToken cancellationToken = default)
        {
            GetTopicsCount++;
            return Task.FromResult(_cachedTopics);
        }

        public Task SetTopicsAsync(
            IReadOnlyCollection<TopicSummaryDto> topics,
            CancellationToken cancellationToken = default)
        {
            SetTopicsCount++;
            return Task.CompletedTask;
        }

        public Task<PagedResult<WordSummaryDto>?> GetTopicWordsAsync(
            uint topicId,
            int page,
            int limit,
            CancellationToken cancellationToken = default)
        {
            GetTopicWordsCount++;
            return Task.FromResult(_cachedTopicWords);
        }

        public Task SetTopicWordsAsync(
            uint topicId,
            int page,
            int limit,
            PagedResult<WordSummaryDto> words,
            CancellationToken cancellationToken = default)
        {
            SetTopicWordsCount++;
            return Task.CompletedTask;
        }

        public Task RemoveTopicsAsync(CancellationToken cancellationToken = default)
        {
            RemoveTopicsCount++;
            return Task.CompletedTask;
        }

        public Task RemoveTopicWordsAsync(uint topicId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
