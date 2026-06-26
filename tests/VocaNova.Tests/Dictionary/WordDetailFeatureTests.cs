using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Dictionary;

public class WordDetailFeatureTests
{
    [Fact]
    public async Task GetByIdAsync_Should_Return_Word_Detail_With_Related_Data()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordDetailAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var word = result.Value!;
        word.WordId.Should().Be(1);
        word.Word.Should().Be("run");
        word.Senses.Should().HaveCount(1);
        word.Senses.Single().Examples.Should().ContainSingle()
            .Which.ExampleEn.Should().Be("I run every morning.");
        word.Senses.Single().Relations.Should().ContainSingle()
            .Which.LinkedWordId.Should().Be(2);

        word.Examples.Should().ContainSingle()
            .Which.SenseId.Should().BeNull();
        word.Relations.Should().ContainSingle()
            .Which.LinkedWordId.Should().BeNull();

        word.Audio.Select(audio => audio.Status).Should().BeEquivalentTo(
            new[] { AudioStatus.Uploaded, AudioStatus.TtsGenerated });
        word.Audio.Should().NotContain(audio => audio.Status == AudioStatus.Pending);
        word.DerivedForms.Should().ContainSingle()
            .Which.LinkedWordId.Should().Be(3);
        word.Idioms.Should().ContainSingle()
            .Which.IdiomText.Should().Be("run out of time");
        word.Topics.Should().ContainSingle()
            .Which.TopicId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_404_When_Word_Is_Deleted()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordDetailAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(4);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Be("Word not found.");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Cached_Detail_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cachedWord = new WordDetailDto(
            10,
            "cached",
            "cached",
            null,
            null,
            null,
            null,
            false,
            Array.Empty<WordSenseDto>(),
            Array.Empty<WordExampleDto>(),
            Array.Empty<WordRelationDto>(),
            Array.Empty<WordAudioDto>(),
            Array.Empty<WordDerivedFormDto>(),
            Array.Empty<WordIdiomDto>(),
            Array.Empty<WordTopicDto>(),
            "active",
            default,
            default);
        var cache = new FakeWordDetailCache(cachedWord);
        var service = CreateService(dbContext, cache);

        var result = await service.GetByIdAsync(10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedWord);
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
        IWordDetailCache? wordDetailCache = null)
    {
        return new WordService(
            new WordRepository(dbContext),
            wordDetailCache: wordDetailCache);
    }

    private static async Task SeedWordDetailAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Topics.Add(new Topic
        {
            TopicId = 1,
            TopicName = "Movement",
            TopicNameVi = "Van dong",
            Icon = "run",
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
            },
            new Word
            {
                WordId = 2,
                Word1 = "sprint",
                WordKey = "sprint",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Word
            {
                WordId = 3,
                Word1 = "running",
                WordKey = "running",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Word
            {
                WordId = 4,
                Word1 = "deleted",
                WordKey = "deleted",
                Status = UserStatus.Deleted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        dbContext.WordSenses.Add(new WordSense
        {
            SenseId = 1,
            WordId = 1,
            SenseOrder = 1,
            WordClass = "verb",
            EnglishDefinition = "move quickly",
            VietnameseMeaning = "chay",
        });
        dbContext.WordExamples.AddRange(
            new WordExample
            {
                ExampleId = 1,
                WordId = 1,
                SenseId = 1,
                ExampleEn = "I run every morning.",
                ExampleVi = "Toi chay moi sang.",
                OrderIndex = 1,
            },
            new WordExample
            {
                ExampleId = 2,
                WordId = 1,
                SenseId = null,
                ExampleEn = "Run the app.",
                ExampleVi = "Chay ung dung.",
                OrderIndex = 1,
            });
        dbContext.WordRelations.AddRange(
            new WordRelation
            {
                RelationId = 1,
                WordId = 1,
                SenseId = 1,
                RelationType = "synonym",
                RelatedWord = "sprint",
                RelatedWordId = 2,
                IsQuizEligible = true,
            },
            new WordRelation
            {
                RelationId = 2,
                WordId = 1,
                SenseId = null,
                RelationType = "antonym",
                RelatedWord = "walk",
                RelatedWordId = null,
                IsQuizEligible = true,
            });
        dbContext.WordAudioAssets.AddRange(
            new WordAudioAsset
            {
                AudioId = 1,
                WordId = 1,
                Accent = "us",
                Source = "upload",
                StorageUrl = "https://example.com/run-us.mp3",
                Status = AudioStatus.Uploaded,
                CreatedAt = DateTime.UtcNow,
            },
            new WordAudioAsset
            {
                AudioId = 2,
                WordId = 1,
                Accent = "uk",
                Source = "tts",
                StorageUrl = "https://example.com/run-uk.mp3",
                Status = AudioStatus.TtsGenerated,
                CreatedAt = DateTime.UtcNow,
            },
            new WordAudioAsset
            {
                AudioId = 3,
                WordId = 1,
                Accent = "au",
                Source = "tts",
                StorageUrl = "https://example.com/run-au.mp3",
                Status = AudioStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            });
        dbContext.WordDerivedForms.Add(new WordDerivedForm
        {
            DerivedId = 1,
            WordId = 1,
            DerivedWord = "running",
            DerivedWordId = 3,
            WordClass = "noun",
        });
        dbContext.WordIdioms.Add(new WordIdiom
        {
            IdiomId = 1,
            WordId = 1,
            IdiomText = "run out of time",
            MeaningEn = "have no time left",
            MeaningVi = "het thoi gian",
        });
        dbContext.WordTopics.Add(new WordTopic
        {
            WordId = 1,
            TopicId = 1,
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeWordDetailCache : IWordDetailCache
    {
        private readonly WordDetailDto? _cachedWord;

        public FakeWordDetailCache(WordDetailDto? cachedWord)
        {
            _cachedWord = cachedWord;
        }

        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public Task<WordDetailDto?> GetAsync(uint wordId, CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(_cachedWord);
        }

        public Task SetAsync(WordDetailDto word, CancellationToken cancellationToken = default)
        {
            SetCount++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint wordId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }
}
