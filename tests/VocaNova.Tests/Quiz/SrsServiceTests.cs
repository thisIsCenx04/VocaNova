using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Features.Quiz.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Quiz;

public class SrsServiceTests
{
    [Fact]
    public async Task UpdateProgressAsync_Should_Insert_New_Progress_When_Missing()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.UpdateProgressAsync(1, 10, isCorrect: true);
        await dbContext.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(1);
        result.Value.WordId.Should().Be(10);
        result.Value.TestCount.Should().Be(1);
        result.Value.CorrectCount.Should().Be(1);
        result.Value.ConsecutiveCorrect.Should().Be(1);
        result.Value.SrsInterval.Should().Be(1);
        result.Value.NextReviewAt.Should().NotBeNull();

        var progress = await dbContext.UserWordProgresses.SingleAsync();
        progress.UserId.Should().Be(1);
        progress.WordId.Should().Be(10);
    }

    [Fact]
    public async Task UpdateProgressAsync_Should_Increase_Mastery_After_Five_Consecutive_Correct()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.UpdateProgressAsync(1, 10, isCorrect: true);
            await dbContext.SaveChangesAsync();
        }

        var progress = await dbContext.UserWordProgresses.SingleAsync();
        progress.TestCount.Should().Be(5);
        progress.CorrectCount.Should().Be(5);
        progress.ConsecutiveCorrect.Should().Be(5);
        progress.MasteryLevel.Should().Be(1);
        progress.NextReviewAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateProgressAsync_Should_Reset_ConsecutiveCorrect_When_Wrong_After_Four_Correct()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await service.UpdateProgressAsync(1, 10, isCorrect: true);
            await dbContext.SaveChangesAsync();
        }

        await service.UpdateProgressAsync(1, 10, isCorrect: false);
        await dbContext.SaveChangesAsync();

        var progress = await dbContext.UserWordProgresses.SingleAsync();
        progress.TestCount.Should().Be(5);
        progress.CorrectCount.Should().Be(4);
        progress.WrongCount.Should().Be(1);
        progress.ConsecutiveCorrect.Should().Be(0);
        progress.IsInWrongList.Should().BeTrue();
        progress.SrsInterval.Should().Be(1);
        progress.LastWrongAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateProgressAsync_Should_Not_Lower_EaseFactor_Below_OnePointThree()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserWordProgresses.Add(new UserWordProgress
        {
            ProgressId = 1,
            UserId = 1,
            WordId = 10,
            TestCount = 10,
            CorrectCount = 0,
            WrongCount = 10,
            ConsecutiveCorrect = 0,
            IsInWrongList = true,
            MasteryLevel = 0,
            SrsInterval = 1,
            EaseFactor = 1.31f,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.UpdateProgressAsync(1, 10, isCorrect: false);
        await dbContext.SaveChangesAsync();

        var progress = await dbContext.UserWordProgresses.SingleAsync();
        progress.EaseFactor.Should().Be(1.3f);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static SrsService CreateService(VocaNovaDbContext dbContext)
    {
        return new SrsService(new SrsRepository(dbContext));
    }
}
