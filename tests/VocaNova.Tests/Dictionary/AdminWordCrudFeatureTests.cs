using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Features.Dictionary.Validators;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Dictionary;

public class AdminWordCrudFeatureTests
{
    [Fact]
    public async Task CreateAsync_Should_Create_Word_With_Normalized_WordKey()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateWordRequest(
            " Run ",
            "a1",
            "/run-uk/",
            "/run-us/",
            "https://example.com/run.png",
            false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Word.Should().Be("Run");
        result.Value.WordKey.Should().Be("run");
        result.Value.Cefr.Should().Be(CefrLevel.A1);

        var word = await dbContext.Words.SingleAsync();
        word.Word1.Should().Be("Run");
        word.WordKey.Should().Be("run");
        word.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_409_When_WordKey_Already_Exists()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateWordRequest(" Run ", CefrLevel.A1, null, null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Word already exists.");
        (await dbContext.Words.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Metadata_And_Invalidate_Word_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var result = await service.UpdateAsync(
            1,
            new UpdateWordRequest(
                " sprint ",
                "b1",
                "/sprint-uk/",
                "/sprint-us/",
                "https://example.com/sprint.png",
                true));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Word.Should().Be("sprint");
        result.Value.WordKey.Should().Be("sprint");
        result.Value.Cefr.Should().Be(CefrLevel.B1);
        result.Value.IsPhrase.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);

        var word = await dbContext.Words.SingleAsync(entity => entity.WordId == 1);
        word.Word1.Should().Be("sprint");
        word.WordKey.Should().Be("sprint");
        word.CefrLevel.Should().Be(CefrLevel.B1);
    }

    [Fact]
    public void CreateWordRequestValidator_Should_Reject_Invalid_Cefr()
    {
        var validator = new CreateWordRequestValidator();

        var result = validator.Validate(new CreateWordRequest("run", "Z9", null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateWordRequest.Cefr));
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

    private static async Task SeedWordAsync(VocaNovaDbContext dbContext, string word, string wordKey)
    {
        dbContext.Words.Add(new Word
        {
            WordId = 1,
            Word1 = word,
            WordKey = wordKey,
            CefrLevel = CefrLevel.A1,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeWordDetailCache : IWordDetailCache
    {
        public int RemoveCount { get; private set; }

        public Task<WordDetailDto?> GetAsync(uint wordId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WordDetailDto?>(null);
        }

        public Task SetAsync(WordDetailDto word, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint wordId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }
}
