using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Dictionary;

public class WordSearchFeatureTests
{
    [Fact]
    public async Task SearchAsync_Should_Return_Words_By_Normalized_Query()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SearchAsync(new WordSearchQuery(" Run ", 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalItems.Should().Be(2);
        result.Value.Items.Select(item => item.Word).Should().Equal("run", "running");

        var run = result.Value.Items.First();
        run.WordId.Should().Be(1);
        run.Phonetic.Should().Be("/run-us/");
        run.Cefr.Should().Be(CefrLevel.A1);
        run.PrimaryMeaning.Should().Be("chay");
        run.ImageUrl.Should().Be("https://example.com/run.png");
    }

    [Fact]
    public async Task SearchAsync_Should_Filter_By_Topic()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SearchAsync(new WordSearchQuery("run", 1, 20)
        {
            TopicId = 2,
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(1);
        result.Value.Items.Single().Word.Should().Be("running");
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Empty_Result_When_No_Words_Match()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SearchAsync(new WordSearchQuery("xyz", 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Cached_Result_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cachedResult = new PagedResult<WordSummaryDto>(
            new[] { new WordSummaryDto(10, "cached", null, null, null, null) },
            1,
            20,
            1);
        var cache = new FakeWordSearchCache(cachedResult);
        var service = CreateService(dbContext, cache);

        var result = await service.SearchAsync(new WordSearchQuery("run", 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedResult);
        cache.GetCount.Should().Be(1);
        cache.SetCount.Should().Be(0);
        (await dbContext.Words.CountAsync()).Should().Be(0);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static WordService CreateService(
        VocaNovaDbContext dbContext,
        IWordSearchCache? wordSearchCache = null)
    {
        return new WordService(
            new WordRepository(dbContext),
            wordSearchCache);
    }

    private static async Task SeedWordsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Topics.AddRange(
            new Topic
            {
                TopicId = 1,
                TopicName = "Movement",
                Status = UserStatus.Active,
            },
            new Topic
            {
                TopicId = 2,
                TopicName = "Sports",
                Status = UserStatus.Active,
            });

        dbContext.Words.AddRange(
            new Word
            {
                WordId = 1,
                Word1 = "run",
                WordKey = "run",
                CefrLevel = CefrLevel.A1,
                PhoneticUk = "/run-uk/",
                PhoneticUs = "/run-us/",
                ImageUrl = "https://example.com/run.png",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                WordSenses =
                {
                    new WordSense
                    {
                        SenseId = 1,
                        WordId = 1,
                        SenseOrder = 2,
                        WordClass = "verb",
                        EnglishDefinition = "move quickly",
                        VietnameseMeaning = "chay nhanh",
                    },
                    new WordSense
                    {
                        SenseId = 2,
                        WordId = 1,
                        SenseOrder = 1,
                        WordClass = "verb",
                        EnglishDefinition = "move",
                        VietnameseMeaning = "chay",
                    },
                },
                WordTopics =
                {
                    new WordTopic
                    {
                        WordId = 1,
                        TopicId = 1,
                    },
                },
            },
            new Word
            {
                WordId = 2,
                Word1 = "running",
                WordKey = "running",
                CefrLevel = CefrLevel.A2,
                PhoneticUs = "/running/",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                WordSenses =
                {
                    new WordSense
                    {
                        SenseId = 3,
                        WordId = 2,
                        SenseOrder = 1,
                        WordClass = "noun",
                        EnglishDefinition = "sport activity",
                        VietnameseMeaning = "chay bo",
                    },
                },
                WordTopics =
                {
                    new WordTopic
                    {
                        WordId = 2,
                        TopicId = 2,
                    },
                },
            },
            new Word
            {
                WordId = 3,
                Word1 = "apple",
                WordKey = "apple",
                CefrLevel = CefrLevel.A1,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Word
            {
                WordId = 4,
                Word1 = "runner",
                WordKey = "runner",
                CefrLevel = CefrLevel.B1,
                Status = UserStatus.Deleted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeWordSearchCache : IWordSearchCache
    {
        private readonly PagedResult<WordSummaryDto>? _cachedResult;

        public FakeWordSearchCache(PagedResult<WordSummaryDto>? cachedResult)
        {
            _cachedResult = cachedResult;
        }

        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public Task<PagedResult<WordSummaryDto>?> GetAsync(
            string cacheKey,
            CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(_cachedResult);
        }

        public Task SetAsync(
            string cacheKey,
            PagedResult<WordSummaryDto> result,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            return Task.CompletedTask;
        }
    }
}
