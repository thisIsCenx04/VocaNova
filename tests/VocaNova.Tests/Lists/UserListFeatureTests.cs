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
    public void CreateListRequestValidator_Should_Reject_Empty_Name()
    {
        var validator = new CreateListRequestValidator();

        var result = validator.Validate(new CreateListRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateListRequest.ListName));
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
