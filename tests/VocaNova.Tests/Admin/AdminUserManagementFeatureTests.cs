using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Features.Admin.Validators;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Admin;

public class AdminUserManagementFeatureTests
{
    [Fact]
    public async Task GetUsersAsync_Should_Search_By_Phone_And_Filter_Status()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(new AdminUserQuery(
            Status: UserStatus.Active,
            Search: "0912"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        var user = result.Value.Items.Single();
        user.UserId.Should().Be(1);
        user.Phone.Should().Be("0912345678");
        user.DisplayName.Should().Be("Nguyen Van A");
    }

    [Fact]
    public async Task GetUserDetailAsync_Should_Return_Profile_And_LearningProfile()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        await SeedLearningLookupsAsync(dbContext);
        dbContext.UserLearningProfiles.Add(new UserLearningProfile
        {
            UserId = 1,
            AgeRangeId = 1,
            RegionId = 1,
            OccupationId = 1,
            EducationLevelId = 1,
            LearningPurposeId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetUserDetailAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Phone.Should().Be("0912345678");
        result.Value.LearningProfile.Should().NotBeNull();
        result.Value.LearningProfile!.AgeRangeName.Should().Be("18-24");
        result.Value.LearningProfile.RegionName.Should().Be("Ho Chi Minh");
        result.Value.LearningProfile.OccupationName.Should().Be("Student");
        result.Value.LearningProfile.EducationLevelName.Should().Be("University");
        result.Value.LearningProfile.LearningPurposeName.Should().Be("Work");
    }

    [Fact]
    public async Task DeactivateAsync_Should_Lock_User_Revoke_RefreshTokens_And_Clear_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        var alreadyRevokedAt = DateTime.UtcNow.AddHours(-1);
        dbContext.RefreshTokens.AddRange(
            new RefreshToken
            {
                TokenId = 1,
                UserId = 1,
                TokenHash = "active-token",
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow,
            },
            new RefreshToken
            {
                TokenId = 2,
                UserId = 1,
                TokenHash = "already-revoked",
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow,
                RevokedAt = alreadyRevokedAt,
            });
        await dbContext.SaveChangesAsync();
        var cache = new FakeUserProfileCache();
        var service = CreateService(dbContext, cache);

        var result = await service.DeactivateAsync(1);

        result.IsSuccess.Should().BeTrue();
        var user = await dbContext.Users.IgnoreQueryFilters().SingleAsync(entity => entity.UserId == 1);
        user.Status.Should().Be(UserStatus.Locked);
        var activeToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenId == 1);
        activeToken.RevokedAt.Should().NotBeNull();
        var preRevokedToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenId == 2);
        preRevokedToken.RevokedAt.Should().Be(alreadyRevokedAt);
        cache.RemovedUserIds.Should().Equal(1u);
    }

    [Fact]
    public async Task RestoreAsync_Should_Restore_Deleted_User()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext, secondUserStatus: UserStatus.Deleted);
        var cache = new FakeUserProfileCache();
        var service = CreateService(dbContext, cache);

        var result = await service.RestoreAsync(2);

        result.IsSuccess.Should().BeTrue();
        var user = await dbContext.Users.SingleAsync(entity => entity.UserId == 2);
        user.Status.Should().Be(UserStatus.Active);
        cache.RemovedUserIds.Should().Equal(2u);
    }

    [Fact]
    public async Task GetUsersAsync_Should_Hide_Deleted_By_Default_And_Include_When_Requested()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext, secondUserStatus: UserStatus.Deleted);
        var service = CreateService(dbContext);

        var hidden = await service.GetUsersAsync(new AdminUserQuery());
        hidden.Value!.Items.Select(item => item.UserId).Should().Equal(1u);

        var included = await service.GetUsersAsync(new AdminUserQuery(IncludeDeleted: true));
        included.Value!.Items.Select(item => item.UserId).Should().BeEquivalentTo(new[] { 1u, 2u });
    }

    [Fact]
    public async Task GetTestHistoryAsync_Should_Return_User_Sessions_Newest_First()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        var today = DateTime.UtcNow.Date;
        dbContext.TestSessions.AddRange(
            NewSession(1, userId: 1, startedAt: today.AddDays(-1), correct: 3, wrong: 1),
            NewSession(2, userId: 1, startedAt: today, correct: 8, wrong: 2),
            // phiên của user khác không được trả về.
            NewSession(3, userId: 2, startedAt: today, correct: 1, wrong: 0));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetTestHistoryAsync(1, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(2);
        result.Value.Items.Select(item => item.SessionId).Should().Equal(2u, 1u);
        result.Value.Items.First().Accuracy.Should().BeApproximately(80f, 0.01f);
    }

    [Fact]
    public async Task GetTestHistoryAsync_Should_Return_NotFound_For_Unknown_User()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetTestHistoryAsync(999, 1, 20);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetUsersAsync_Should_Expose_GoogleEmail_And_Filter_By_Role()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var all = await service.GetUsersAsync(new AdminUserQuery());
        all.Value!.Items.Should().Contain(item => item.UserId == 1 && item.GoogleEmail == "a@example.com");

        var admins = await service.GetUsersAsync(new AdminUserQuery(Role: UserRole.Admin));
        admins.Value!.Items.Select(item => item.UserId).Should().Equal(2u);
    }

    [Fact]
    public async Task GetUserTopicsAsync_Should_Split_Selected_And_Suggested()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        dbContext.Topics.AddRange(
            new Topic { TopicId = 1, TopicName = "Business", Status = UserStatus.Active },
            new Topic { TopicId = 2, TopicName = "Travel", Status = UserStatus.Active },
            new Topic { TopicId = 3, TopicName = "Science", Status = UserStatus.Active },
            new Topic { TopicId = 4, TopicName = "Old", Status = UserStatus.Active });
        var now = DateTime.UtcNow;
        dbContext.UserTopicPreferences.AddRange(
            new UserTopicPreference { UserId = 1, TopicId = 1, Source = "user_selected", Status = UserStatus.Active, CreatedAt = now },
            new UserTopicPreference { UserId = 1, TopicId = 2, Source = "onboarding", Status = UserStatus.Active, CreatedAt = now },
            new UserTopicPreference { UserId = 1, TopicId = 3, Source = "knn_suggested", Status = UserStatus.Active, CreatedAt = now },
            // deleted pref bị bỏ qua
            new UserTopicPreference { UserId = 1, TopicId = 4, Source = "user_selected", Status = UserStatus.Deleted, CreatedAt = now });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetUserTopicsAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Selected.Select(t => t.Name).Should().BeEquivalentTo(new[] { "Business", "Travel" });
        result.Value.Suggested.Select(t => t.Name).Should().Equal("Science");
    }

    [Fact]
    public async Task GetUserTopicsAsync_Should_Return_NotFound_For_Unknown_User()
    {
        await using var dbContext = CreateDbContext();
        await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetUserTopicsAsync(999);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public void AdminUserQueryValidator_Should_Reject_Invalid_Status_And_Paging()
    {
        var validator = new AdminUserQueryValidator();

        var result = validator.TestValidate(new AdminUserQuery(Page: 0, Limit: 101, Status: "archived"));

        result.ShouldHaveValidationErrorFor(query => query.Page);
        result.ShouldHaveValidationErrorFor(query => query.Limit);
        result.ShouldHaveValidationErrorFor(query => query.Status);
    }

    private static AdminUserService CreateService(
        VocaNovaDbContext dbContext,
        IUserProfileCache? userProfileCache = null)
    {
        return new AdminUserService(
            new AdminUserRepository(dbContext),
            userProfileCache);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static async Task SeedUsersAsync(
        VocaNovaDbContext dbContext,
        string secondUserStatus = UserStatus.Active)
    {
        dbContext.Roles.AddRange(
            new Role { RoleId = 1, RoleName = UserRole.User },
            new Role { RoleId = 2, RoleName = UserRole.Admin });

        dbContext.Users.AddRange(
            new User
            {
                UserId = 1,
                RoleId = 1,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                UserAuth = new UserAuth
                {
                    UserId = 1,
                    Phone = "0912345678",
                    GoogleEmail = "a@example.com",
                    IsPhoneVerified = true,
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                },
                UserProfile = new UserProfile
                {
                    UserId = 1,
                    FullName = "Nguyen Van A",
                    AvatarUrl = "https://example.com/a.png",
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                },
            },
            new User
            {
                UserId = 2,
                RoleId = 2,
                Status = secondUserStatus,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                UserAuth = new UserAuth
                {
                    UserId = 2,
                    Phone = "0987654321",
                    IsPhoneVerified = true,
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                },
                UserProfile = new UserProfile
                {
                    UserId = 2,
                    FullName = "Tran Thi B",
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                },
            });

        await dbContext.SaveChangesAsync();
    }

    private static TestSession NewSession(uint sessionId, uint userId, DateTime startedAt, int correct, int wrong)
    {
        return new TestSession
        {
            SessionId = sessionId,
            UserId = userId,
            TestType = "multiple_choice",
            Mode = "standard",
            ScopeType = "all",
            WordOrder = "random",
            QuestionType = 1,
            QuestionCount = correct + wrong,
            CorrectCount = correct,
            WrongCount = wrong,
            Score = correct,
            MaxStreak = correct,
            Status = TestSessionStatus.Completed,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(5),
        };
    }

    private static async Task SeedLearningLookupsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.AgeRanges.Add(new AgeRange { AgeRangeId = 1, Name = "18-24", DisplayOrder = 1, Status = UserStatus.Active });
        dbContext.Regions.Add(new Region { RegionId = 1, Name = "Ho Chi Minh", Code = "HCM", Status = UserStatus.Active });
        dbContext.Occupations.Add(new Occupation { OccupationId = 1, Name = "Student", Status = UserStatus.Active });
        dbContext.EducationLevels.Add(new EducationLevel { EducationLevelId = 1, Name = "University", DisplayOrder = 1, Status = UserStatus.Active });
        dbContext.LearningPurposes.Add(new LearningPurpose { LearningPurposeId = 1, Name = "Work", Status = UserStatus.Active });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeUserProfileCache : IUserProfileCache
    {
        public List<uint> RemovedUserIds { get; } = new();

        public Task<UserProfileDto?> GetAsync(uint userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserProfileDto?>(null);
        }

        public Task SetAsync(UserProfileDto profile, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemovedUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }
}
