using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.Contracts.Requests;
using VocaNova.API.Features.Dictionary.Contracts.Responses;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.DAL.Repositories;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Dictionary;

public class AdminWordListFeatureTests
{
    [Fact]
    public async Task SearchAdminAsync_Should_Return_Only_Active_When_Not_Including_Deleted()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SearchAdminAsync(new AdminWordQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(item => item.Word).Should().NotContain("runner");
        result.Value.Items.Select(item => item.Word).Should().Equal("apple", "desert", "run", "running");
    }

    [Fact]
    public async Task SearchAdminAsync_Should_Include_Deleted_When_Requested()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SearchAdminAsync(new AdminWordQuery { IncludeDeleted = true });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().Contain(item => item.Word == "runner" && item.Status == UserStatus.Deleted);
    }

    [Fact]
    public async Task SearchAdminAsync_Status_Deleted_Should_Force_Include_Deleted_And_Filter()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext);
        var service = CreateService(dbContext);

        // status='deleted' nhưng includeDeleted=false → service vẫn bỏ global filter để thấy deleted.
        var result = await service.SearchAdminAsync(new AdminWordQuery { Status = "deleted" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(item => item.Status == UserStatus.Deleted);
        result.Value.Items.Select(item => item.Word).Should().Equal("runner");
    }

    [Fact]
    public async Task SearchAdminAsync_Should_Filter_By_Query_Cefr_Topic_And_Project_Row()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SearchAdminAsync(new AdminWordQuery
        {
            Q = "run",
            Cefr = "a1",
            TopicId = 1,
        });

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Items.Should().ContainSingle().Subject;
        row.Word.Should().Be("run");
        row.Cefr.Should().Be(CefrLevel.A1);
        row.Phonetic.Should().Be("/run-us/");
        row.PrimaryMeaning.Should().Be("chay"); // sense order 1
        row.WordType.Should().Be("verb"); // word_class của sense chính
        row.Topics.Should().ContainSingle(topic => topic.TopicId == 1 && topic.Name == "Movement");
    }

    [Fact]
    public async Task SearchAdminAsync_Should_Filter_By_WordType()
    {
        await using var dbContext = CreateDbContext();
        await SeedAsync(dbContext);
        var service = CreateService(dbContext);

        var verbs = await service.SearchAdminAsync(new AdminWordQuery { WordType = "verb" });

        verbs.IsSuccess.Should().BeTrue();
        verbs.Value!.Items.Should().OnlyContain(item => item.WordType == "verb");
        verbs.Value.Items.Select(item => item.Word).Should().Contain("run");

        var nouns = await service.SearchAdminAsync(new AdminWordQuery { WordType = "noun" });
        var desert = nouns.Value!.Items.Should().ContainSingle(item => item.Word == "desert").Subject;
        desert.WordType.Should().Be("noun");
        desert.PrimaryMeaning.Should().Be("sa mac");
    }

    [Fact]
    public async Task SearchAdminAsync_Should_Reject_Invalid_Cefr_And_Status()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        (await service.SearchAdminAsync(new AdminWordQuery { Cefr = "Z9" })).IsSuccess.Should().BeFalse();
        (await service.SearchAdminAsync(new AdminWordQuery { Status = "locked" })).IsSuccess.Should().BeFalse();
        (await service.SearchAdminAsync(new AdminWordQuery { Page = 0 })).IsSuccess.Should().BeFalse();
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static WordService CreateService(VocaNovaDbContext dbContext)
    {
        return new WordService(new WordRepository(dbContext));
    }

    private static async Task SeedAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Topics.AddRange(
            new Topic { TopicId = 1, TopicName = "Movement", TopicNameVi = "Chuyển động", Icon = "run", Status = UserStatus.Active },
            new Topic { TopicId = 2, TopicName = "Sports", Status = UserStatus.Active });

        dbContext.Words.AddRange(
            new Word
            {
                WordId = 1,
                Word1 = "run",
                WordKey = "run",
                CefrLevel = CefrLevel.A1,
                PhoneticUk = "/run-uk/",
                PhoneticUs = "/run-us/",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                WordSenses =
                {
                    new EntityWordSense { SenseId = 1, WordId = 1, SenseOrder = 2, WordClass = "verb", EnglishDefinition = "move quickly", VietnameseMeaning = "chay nhanh" },
                    new EntityWordSense { SenseId = 2, WordId = 1, SenseOrder = 1, WordClass = "verb", EnglishDefinition = "move", VietnameseMeaning = "chay" },
                },
                WordTopics = { new EntityWordTopic { WordId = 1, TopicId = 1 } },
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
                WordTopics = { new EntityWordTopic { WordId = 2, TopicId = 2 } },
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
            },
            new Word
            {
                WordId = 5,
                Word1 = "desert",
                WordKey = "desert",
                CefrLevel = CefrLevel.A2,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                WordSenses =
                {
                    new EntityWordSense { SenseId = 5, WordId = 5, SenseOrder = 1, WordClass = "adjective", EnglishDefinition = "abandoned", VietnameseMeaning = "hoang vang" },
                    new EntityWordSense { SenseId = 6, WordId = 5, SenseOrder = 2, WordClass = "noun", EnglishDefinition = "dry region", VietnameseMeaning = "sa mac" },
                },
            });

        await dbContext.SaveChangesAsync();
    }
}
