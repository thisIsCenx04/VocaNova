using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Shared;

public class GlobalQueryFilterTests
{
    [Fact]
    public async Task UserLists_Should_Exclude_Deleted_Rows_By_Default()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserLists.AddRange(
            new UserList
            {
                ListId = 1,
                UserId = 1,
                ListName = "Active list",
                Status = UserStatus.Active,
            },
            new UserList
            {
                ListId = 2,
                UserId = 1,
                ListName = "Deleted list",
                Status = UserStatus.Deleted,
            });
        await dbContext.SaveChangesAsync();

        var lists = await dbContext.UserLists
            .OrderBy(list => list.ListId)
            .ToListAsync();

        lists.Should().ContainSingle();
        lists[0].ListName.Should().Be("Active list");
    }

    [Fact]
    public async Task UserLists_Should_Include_Deleted_Rows_When_Query_Filters_Are_Ignored()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserLists.AddRange(
            new UserList
            {
                ListId = 1,
                UserId = 1,
                ListName = "Active list",
                Status = UserStatus.Active,
            },
            new UserList
            {
                ListId = 2,
                UserId = 1,
                ListName = "Deleted list",
                Status = UserStatus.Deleted,
            });
        await dbContext.SaveChangesAsync();

        var lists = await dbContext.UserLists
            .IgnoreQueryFilters()
            .OrderBy(list => list.ListId)
            .ToListAsync();

        lists.Should().HaveCount(2);
        lists.Select(list => list.Status)
            .Should()
            .BeEquivalentTo(new[] { UserStatus.Active, UserStatus.Deleted });
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }
}
