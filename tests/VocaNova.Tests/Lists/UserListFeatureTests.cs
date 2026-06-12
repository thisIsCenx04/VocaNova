using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Repositories;
using VocaNova.API.Features.Lists.Services;
using VocaNova.API.Features.Lists.Validators;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Lists;

public class UserListFeatureTests
{
    [Fact]
    public async Task GetByUserAsync_Should_Return_Active_Lists_With_WordCount()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetByUserAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var lists = result.Value!;
        lists.Select(list => list.ListName).Should().Equal("Travel", "Favorites");
        lists.Single(list => list.ListName == "Favorites").WordCount.Should().Be(2);
        lists.Should().NotContain(list => list.ListName == "Deleted");
    }

    [Fact]
    public async Task GetByUserAsync_Should_Return_Cached_Lists_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cachedLists = new[]
        {
            new UserListDto(99, "Cached", 3, DateTime.UtcNow),
        };
        var cache = new FakeUserListCache(cachedLists);
        var service = CreateService(dbContext, cache);

        var result = await service.GetByUserAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedLists);
        cache.GetCount.Should().Be(1);
        cache.SetCount.Should().Be(0);
        (await dbContext.UserLists.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_List_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.CreateAsync(1, new CreateListRequest(" Favorites "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ListName.Should().Be("Favorites");
        result.Value.WordCount.Should().Be(0);
        cache.RemoveCount.Should().Be(1);

        var list = await dbContext.UserLists.SingleAsync();
        list.UserId.Should().Be(1);
        list.ListName.Should().Be("Favorites");
        list.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_400_When_User_Already_Has_50_Active_Lists()
    {
        await using var dbContext = CreateDbContext();
        for (var index = 1; index <= AppSettings.MaxListsPerUser; index++)
        {
            dbContext.UserLists.Add(new UserList
            {
                ListId = (uint)index,
                UserId = 1,
                ListName = $"List {index}",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMinutes(index),
            });
        }

        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(1, new CreateListRequest("Overflow"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("A user can create at most 50 lists.");
        (await dbContext.UserLists.CountAsync()).Should().Be(AppSettings.MaxListsPerUser);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_409_When_ListName_Already_Exists_CaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserLists.Add(new UserList
        {
            ListId = 1,
            UserId = 1,
            ListName = "Favorites",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(1, new CreateListRequest(" favorites "));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("List name already exists.");
        (await dbContext.UserLists.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Should_Rename_List_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.UpdateAsync(1, 1, new UpdateListRequest(" Favorites Updated "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ListName.Should().Be("Favorites Updated");
        result.Value.WordCount.Should().Be(2);
        cache.RemoveCount.Should().Be(1);

        var list = await dbContext.UserLists.SingleAsync(entity => entity.ListId == 1);
        list.ListName.Should().Be("Favorites Updated");
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_409_When_NewName_Already_Exists_CaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(1, 1, new UpdateListRequest(" travel "));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("List name already exists.");

        var list = await dbContext.UserLists.SingleAsync(entity => entity.ListId == 1);
        list.ListName.Should().Be("Favorites");
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Return_403_When_User_Does_Not_Own_List()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SoftDeleteAsync(1, 4);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Be("You do not have access to this list.");

        var list = await dbContext.UserLists.SingleAsync(entity => entity.ListId == 4);
        list.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Delete_List_And_Cascade_Delete_ListWords_Without_Changing_Progress()
    {
        await using var dbContext = CreateDbContext();
        await SeedListWithWordsAndProgressAsync(dbContext, listWordCount: 10);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.SoftDeleteAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);

        var list = await dbContext.UserLists
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.ListId == 10);
        list.Status.Should().Be(UserStatus.Deleted);

        var listWords = await dbContext.UserListWords
            .IgnoreQueryFilters()
            .Where(entity => entity.ListId == 10)
            .ToListAsync();
        listWords.Should().HaveCount(10);
        listWords.Should().OnlyContain(listWord => listWord.Status == UserStatus.Deleted);

        var progress = await dbContext.UserWordProgresses
            .OrderBy(entity => entity.WordId)
            .ToListAsync();
        progress.Should().HaveCount(10);
        progress.Should().OnlyContain(item => item.TestCount == 5 && item.CorrectCount == 3 && item.WrongCount == 2);
    }

    [Fact]
    public void CreateListRequestValidator_Should_Reject_Empty_Name()
    {
        var validator = new CreateListRequestValidator();

        var result = validator.Validate(new CreateListRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateListRequest.ListName));
    }

    [Fact]
    public void UpdateListRequestValidator_Should_Reject_Empty_Name()
    {
        var validator = new UpdateListRequestValidator();

        var result = validator.Validate(new UpdateListRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateListRequest.ListName));
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static UserListService CreateService(
        VocaNovaDbContext dbContext,
        IUserListCache? userListCache = null)
    {
        return new UserListService(
            new UserListRepository(dbContext),
            userListCache);
    }

    private static async Task SeedListsAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.AddRange(
            new UserList
            {
                ListId = 1,
                UserId = 1,
                ListName = "Favorites",
                Status = UserStatus.Active,
                CreatedAt = now.AddMinutes(-10),
            },
            new UserList
            {
                ListId = 2,
                UserId = 1,
                ListName = "Travel",
                Status = UserStatus.Active,
                CreatedAt = now,
            },
            new UserList
            {
                ListId = 3,
                UserId = 1,
                ListName = "Deleted",
                Status = UserStatus.Deleted,
                CreatedAt = now.AddMinutes(-5),
            },
            new UserList
            {
                ListId = 4,
                UserId = 2,
                ListName = "Other User",
                Status = UserStatus.Active,
                CreatedAt = now,
            });

        dbContext.UserListWords.AddRange(
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 1,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now,
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 2,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now,
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 3,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Deleted,
                AddedAt = now,
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedListWithWordsAndProgressAsync(
        VocaNovaDbContext dbContext,
        int listWordCount)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 10,
            UserId = 1,
            ListName = "Cascade",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        for (var index = 1; index <= listWordCount; index++)
        {
            dbContext.UserListWords.Add(new UserListWord
            {
                UserId = 1,
                ListId = 10,
                WordId = (uint)index,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now.AddMinutes(index),
            });

            dbContext.UserWordProgresses.Add(new UserWordProgress
            {
                ProgressId = (uint)index,
                UserId = 1,
                WordId = (uint)index,
                TestCount = 5,
                CorrectCount = 3,
                WrongCount = 2,
                ConsecutiveCorrect = 1,
                IsInWrongList = false,
                MasteryLevel = 2,
                SrsInterval = 1,
                EaseFactor = 2.5f,
                UpdatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeUserListCache : IUserListCache
    {
        private readonly IReadOnlyCollection<UserListDto>? _cachedLists;

        public FakeUserListCache(IReadOnlyCollection<UserListDto>? cachedLists = null)
        {
            _cachedLists = cachedLists;
        }

        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public Task<IReadOnlyCollection<UserListDto>?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(_cachedLists);
        }

        public Task SetAsync(
            uint userId,
            IReadOnlyCollection<UserListDto> lists,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }
}
