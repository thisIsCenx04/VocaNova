using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Features.Admin.Validators;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Admin;

public class AdminStatsFeatureTests
{
    [Fact]
    public async Task GetDashboardAsync_Should_Return_Counts_And_Cache_For_Five_Minutes()
    {
        await using var dbContext = CreateDbContext();
        var today = DateTime.UtcNow.Date;
        await SeedUsersAsync(dbContext);
        dbContext.Words.Add(new Word
        {
            WordId = 1,
            Word1 = "apple",
            WordKey = "apple",
            Status = UserStatus.Active,
            CreatedAt = today,
            UpdatedAt = today,
        });
        dbContext.TestSessions.Add(new TestSession
        {
            SessionId = 1,
            UserId = 1,
            TestType = "quiz",
            Mode = "practice",
            ScopeType = "all",
            WordOrder = "random",
            QuestionCount = 10,
            CorrectCount = 8,
            WrongCount = 2,
            StartedAt = today.AddHours(1),
            Status = TestSessionStatus.Completed,
        });
        await dbContext.SaveChangesAsync();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(dbContext, cache);

        var first = await service.GetDashboardAsync();
        dbContext.Words.Add(new Word
        {
            WordId = 2,
            Word1 = "banana",
            WordKey = "banana",
            Status = UserStatus.Active,
            CreatedAt = today,
            UpdatedAt = today,
        });
        await dbContext.SaveChangesAsync();
        var second = await service.GetDashboardAsync();

        first.IsSuccess.Should().BeTrue();
        first.Value!.TotalUsers.Should().Be(2);
        first.Value.TotalWords.Should().Be(1);
        first.Value.SessionsToday.Should().Be(1);
        first.Value.AvgAccuracy7d.Should().Be(80);
        second.Value!.TotalWords.Should().Be(first.Value.TotalWords);
    }

    [Fact]
    public async Task GetDemographicsAsync_Should_Group_Active_User_Learning_Profile_Data()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        await SeedLookupsAsync(dbContext);
        var now = DateTime.UtcNow;
        dbContext.UserLearningProfiles.AddRange(
            new UserLearningProfile
            {
                UserId = 1,
                AgeRangeId = 1,
                OccupationId = 1,
                EducationLevelId = 1,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new UserLearningProfile
            {
                UserId = 2,
                AgeRangeId = 1,
                OccupationId = 2,
                EducationLevelId = 1,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new UserLearningProfile
            {
                UserId = 3,
                AgeRangeId = 2,
                OccupationId = 1,
                EducationLevelId = 2,
                CreatedAt = now,
                UpdatedAt = now,
            });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetDemographicsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.AgeRanges.Should().ContainSingle(group => group.Id == 1 && group.UserCount == 2);
        result.Value.AgeRanges.Should().NotContain(group => group.Id == 2);
        result.Value.Occupations.Should().Contain(group => group.Name == "Student" && group.UserCount == 1);
        result.Value.Occupations.Should().Contain(group => group.Name == "Engineer" && group.UserCount == 1);
        result.Value.EducationLevels.Should().ContainSingle(group => group.Name == "University" && group.UserCount == 2);
    }

    [Fact]
    public async Task GetLearningStatsAsync_Should_Return_Top_Wrong_Words_And_Thirty_Day_Trend()
    {
        await using var dbContext = CreateDbContext();
        var today = DateTime.UtcNow.Date;
        await SeedUsersAsync(dbContext);
        dbContext.Words.AddRange(
            new Word
            {
                WordId = 1,
                Word1 = "apple",
                WordKey = "apple",
                Status = UserStatus.Active,
                CreatedAt = today,
                UpdatedAt = today,
            },
            new Word
            {
                WordId = 2,
                Word1 = "banana",
                WordKey = "banana",
                Status = UserStatus.Active,
                CreatedAt = today,
                UpdatedAt = today,
            });
        dbContext.TestSessions.AddRange(
            CreateSession(1, 1, today, 1, 2),
            CreateSession(2, 1, today.AddDays(-1), 3, 1));
        dbContext.TestAnswers.AddRange(
            CreateAnswer(1, 1, 1, false),
            CreateAnswer(2, 1, 1, false),
            CreateAnswer(3, 1, 2, true),
            CreateAnswer(4, 2, 1, true),
            CreateAnswer(5, 2, 2, false));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetLearningStatsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.TopWrongWords.First().Word.Should().Be("apple");
        result.Value.TopWrongWords.First().WrongCount.Should().Be(2);
        result.Value.TopWrongWords.First().Accuracy.Should().BeApproximately(33.33, 0.01);
        result.Value.AccuracyTrend.Should().HaveCount(30);
        result.Value.AccuracyTrend.Last().Date.Should().Be(DateOnly.FromDateTime(today));
        result.Value.AccuracyTrend.Last().CorrectCount.Should().Be(1);
        result.Value.AccuracyTrend.Last().WrongCount.Should().Be(2);
        result.Value.AccuracyTrend.Last().Accuracy.Should().BeApproximately(33.33, 0.01);
    }

    [Fact]
    public async Task GetSessionsTrendAsync_Should_Fill_Requested_Days_With_Session_Counts()
    {
        await using var dbContext = CreateDbContext();
        var today = DateTime.UtcNow.Date;
        await SeedUsersAsync(dbContext);
        dbContext.TestSessions.AddRange(
            CreateSession(1, 1, today.AddHours(2), 5, 1),
            CreateSession(2, 1, today.AddHours(4), 3, 2),
            CreateSession(3, 1, today.AddDays(-2).AddHours(1), 4, 0));
        // Phiên ngoài cửa sổ 7 ngày không được đếm.
        dbContext.TestSessions.Add(CreateSession(4, 1, today.AddDays(-10), 1, 0));
        // Phiên chưa hoàn thành bị loại.
        var pending = CreateSession(5, 1, today.AddHours(6), 0, 0);
        pending.Status = TestSessionStatus.InProgress;
        dbContext.TestSessions.Add(pending);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetSessionsTrendAsync(7);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Days.Should().Be(7);
        result.Value.Points.Should().HaveCount(7);
        result.Value.Points.Last().Date.Should().Be(DateOnly.FromDateTime(today));
        result.Value.Points.Last().SessionCount.Should().Be(2);
        result.Value.Points.Single(point => point.Date == DateOnly.FromDateTime(today.AddDays(-2)))
            .SessionCount.Should().Be(1);
        result.Value.Points.Single(point => point.Date == DateOnly.FromDateTime(today.AddDays(-1)))
            .SessionCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSessionsTrendAsync_Should_Reject_Invalid_Days()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        (await service.GetSessionsTrendAsync(0)).IsSuccess.Should().BeFalse();
        (await service.GetSessionsTrendAsync(91)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetMasteryDistributionAsync_Should_Return_Levels_0_To_5_Excluding_Deleted_Users()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext); // user 1 active, 2 locked, 3 deleted
        dbContext.UserWordProgresses.AddRange(
            new UserWordProgress { ProgressId = 1, UserId = 1, WordId = 1, MasteryLevel = 0, EaseFactor = 2.5f, UpdatedAt = DateTime.UtcNow },
            new UserWordProgress { ProgressId = 2, UserId = 1, WordId = 2, MasteryLevel = 5, EaseFactor = 2.5f, UpdatedAt = DateTime.UtcNow },
            new UserWordProgress { ProgressId = 3, UserId = 2, WordId = 1, MasteryLevel = 5, EaseFactor = 2.5f, UpdatedAt = DateTime.UtcNow },
            // Tiến độ của user đã xoá bị loại khỏi thống kê.
            new UserWordProgress { ProgressId = 4, UserId = 3, WordId = 1, MasteryLevel = 3, EaseFactor = 2.5f, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetMasteryDistributionAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Levels.Should().HaveCount(6);
        result.Value.TotalWordsInProgress.Should().Be(3);
        result.Value.Levels.Single(level => level.Level == 0).WordCount.Should().Be(1);
        result.Value.Levels.Single(level => level.Level == 5).WordCount.Should().Be(2);
        result.Value.Levels.Single(level => level.Level == 3).WordCount.Should().Be(0);
    }

    [Fact]
    public async Task GetActivityTrendAsync_Daily_Should_Return_30_Buckets_With_Sessions_And_Accuracy()
    {
        await using var dbContext = CreateDbContext();
        var today = DateTime.UtcNow.Date;
        await SeedUsersAsync(dbContext);
        dbContext.TestSessions.AddRange(
            CreateSession(1, 1, today.AddHours(1), 8, 2),
            CreateSession(2, 1, today.AddHours(3), 6, 4));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetActivityTrendAsync("daily");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Granularity.Should().Be("daily");
        result.Value.Points.Should().HaveCount(30);
        var last = result.Value.Points.Last();
        last.Period.Should().Be(today.ToString("yyyy-MM-dd"));
        last.SessionsCount.Should().Be(2);
        last.CorrectCount.Should().Be(14);
        last.TotalCount.Should().Be(20);
        last.Accuracy.Should().Be(70);
    }

    [Fact]
    public async Task GetActivityTrendAsync_Should_Return_Expected_Bucket_Counts_And_Reject_Invalid()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        (await service.GetActivityTrendAsync("weekly")).Value!.Points.Should().HaveCount(12);
        (await service.GetActivityTrendAsync("monthly")).Value!.Points.Should().HaveCount(6);
        (await service.GetActivityTrendAsync("yearly")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuditLogsAsync_Should_Filter_By_User_And_Entity_With_Pagination()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        var now = DateTime.UtcNow;
        dbContext.AuditLogs.AddRange(
            new AuditLog
            {
                LogId = 1,
                UserId = 1,
                Action = "PATCH",
                EntityType = "users",
                EntityId = 2,
                IpAddress = "127.0.0.1",
                CreatedAt = now.AddMinutes(-2),
            },
            new AuditLog
            {
                LogId = 2,
                UserId = 1,
                Action = "POST",
                EntityType = "words",
                EntityId = 1,
                CreatedAt = now.AddMinutes(-1),
            },
            new AuditLog
            {
                LogId = 3,
                UserId = 2,
                Action = "PATCH",
                EntityType = "users",
                EntityId = 1,
                CreatedAt = now,
            });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetAuditLogsAsync(new AdminAuditLogQuery(
            Page: 1,
            Limit: 1,
            UserId: 1,
            Entity: " users "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(1);
        result.Value.Items.Single().LogId.Should().Be(1);
        result.Value.Items.Single().EntityType.Should().Be("users");
    }

    [Fact]
    public void AdminAuditLogQueryValidator_Should_Reject_Invalid_Query()
    {
        var validator = new AdminAuditLogQueryValidator();

        var result = validator.TestValidate(new AdminAuditLogQuery(
            Page: 0,
            Limit: 101,
            Entity: new string('a', 51)));

        result.ShouldHaveValidationErrorFor(query => query.Page);
        result.ShouldHaveValidationErrorFor(query => query.Limit);
        result.ShouldHaveValidationErrorFor(query => query.Entity);
    }

    private static AdminStatsService CreateService(
        VocaNovaDbContext dbContext,
        IMemoryCache? cache = null)
    {
        return new AdminStatsService(
            new AdminStatsRepository(dbContext),
            cache ?? new MemoryCache(new MemoryCacheOptions()));
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static async Task SeedUsersAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Roles.Add(new Role { RoleId = 1, RoleName = UserRole.User });
        var now = DateTime.UtcNow;
        dbContext.Users.AddRange(
            new User
            {
                UserId = 1,
                RoleId = 1,
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new User
            {
                UserId = 2,
                RoleId = 1,
                Status = UserStatus.Locked,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new User
            {
                UserId = 3,
                RoleId = 1,
                Status = UserStatus.Deleted,
                CreatedAt = now,
                UpdatedAt = now,
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedLookupsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.AgeRanges.AddRange(
            new AgeRange { AgeRangeId = 1, Name = "18-24", DisplayOrder = 1, Status = UserStatus.Active },
            new AgeRange { AgeRangeId = 2, Name = "25-34", DisplayOrder = 2, Status = UserStatus.Active });
        dbContext.Occupations.AddRange(
            new Occupation { OccupationId = 1, Name = "Student", Status = UserStatus.Active },
            new Occupation { OccupationId = 2, Name = "Engineer", Status = UserStatus.Active });
        dbContext.EducationLevels.AddRange(
            new EducationLevel { EducationLevelId = 1, Name = "University", DisplayOrder = 1, Status = UserStatus.Active },
            new EducationLevel { EducationLevelId = 2, Name = "High School", DisplayOrder = 2, Status = UserStatus.Active });

        await dbContext.SaveChangesAsync();
    }

    private static TestSession CreateSession(
        uint sessionId,
        uint userId,
        DateTime startedAt,
        int correctCount,
        int wrongCount)
    {
        return new TestSession
        {
            SessionId = sessionId,
            UserId = userId,
            TestType = "quiz",
            Mode = "practice",
            ScopeType = "all",
            WordOrder = "random",
            QuestionCount = correctCount + wrongCount,
            CorrectCount = correctCount,
            WrongCount = wrongCount,
            StartedAt = startedAt,
            Status = TestSessionStatus.Completed,
        };
    }

    private static TestAnswer CreateAnswer(
        uint answerId,
        uint sessionId,
        uint wordId,
        bool isCorrect)
    {
        return new TestAnswer
        {
            AnswerId = answerId,
            SessionId = sessionId,
            WordId = wordId,
            QuestionNumber = (int)answerId,
            DisplayContent = "Question",
            ExpectedAnswer = "Answer",
            IsCorrect = isCorrect,
        };
    }
}
