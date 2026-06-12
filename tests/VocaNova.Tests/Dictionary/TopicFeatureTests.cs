using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Features.Dictionary.Validators;
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
    public async Task SoftDeleteAsync_Should_Return_409_When_Topic_Has_Active_Words()
    {
        await using var dbContext = CreateDbContext();
        await SeedTopicsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SoftDeleteAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Topic still has active words.");

        var topic = await dbContext.Topics.SingleAsync(entity => entity.TopicId == 1);
        topic.Status.Should().Be(UserStatus.Active);
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
    }
}
