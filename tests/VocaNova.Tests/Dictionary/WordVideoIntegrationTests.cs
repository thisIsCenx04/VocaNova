using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.DAL.Repositories;

namespace VocaNova.Tests.Dictionary;

public sealed class WordVideoIntegrationTests
{
    [Fact]
    public async Task MySql_Video_Replacement_Reuses_Row_And_Soft_Delete_Hides_Video_Only()
    {
        EnvironmentFile.LoadFromRepositoryRoot();
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseMySql(DatabaseConnection.GetConnectionString(), DatabaseConnection.GetServerVersion()).Options;
        await using var db = new VocaNovaDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var name = $"video-test-{Guid.NewGuid():N}";
        var word = new EntityWord { Word1 = name, WordKey = name, Status = "active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Words.Add(word);
        await db.SaveChangesAsync();
        var repository = new WordVideoRepository(db);
        var first = await repository.ReplaceAsync(word.WordId, new StoredWordVideo("first", "https://example.test/first.mp4", "https://example.test/first.jpg"));
        first!.PreviousPublicId.Should().BeNull();
        var created = (await db.WordVideoAssets.SingleAsync(v => v.WordId == word.WordId)).CreatedAt;
        (await new WordReadRepository(db).FindDetailAsync(word.WordId))!.Video!.Url.Should().EndWith("first.mp4");
        (await repository.SoftDeleteAsync(word.WordId)).Should().BeTrue();
        (await repository.IsReferencedAsync("first")).Should().BeTrue();
        (await new WordReadRepository(db).FindDetailAsync(word.WordId))!.Video.Should().BeNull();
        (await repository.SoftDeleteAsync(word.WordId)).Should().BeFalse();
        var replacement = await repository.ReplaceAsync(word.WordId, new StoredWordVideo("second", "https://example.test/second.mp4", "https://example.test/second.jpg"));
        replacement!.PreviousPublicId.Should().Be("first");
        replacement.Video.VideoId.Should().Be(first.Video.VideoId);
        var row = await db.WordVideoAssets.IgnoreQueryFilters().SingleAsync(v => v.WordId == word.WordId);
        row.CreatedAt.Should().Be(created);
        row.Status.Should().Be("active");
        (await new WordReadRepository(db).FindDetailAsync(word.WordId))!.Video!.Url.Should().EndWith("second.mp4");
        await transaction.RollbackAsync();
    }
}
