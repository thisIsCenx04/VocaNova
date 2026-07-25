using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Features.SuperAdmin.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.SuperAdmin;

public sealed class AdminUserAssignmentFeatureTests
{
    [Fact]
    public async Task ReplaceAsync_Should_Reject_User_Assigned_To_Another_Admin()
    {
        await using var db = CreateDbContext();
        await SeedAsync(db);
        var store = new MemoryAssignmentStore();
        var service = new AdminUserAssignmentService(db, store);

        (await service.ReplaceAsync(20, [10])).IsSuccess.Should().BeTrue();
        var conflict = await service.ReplaceAsync(21, [10, 11]);

        conflict.IsSuccess.Should().BeFalse();
        conflict.StatusCode.Should().Be(409);
        (await store.GetUserIdsAsync(20)).Should().BeEquivalentTo([10u]);
        (await store.GetUserIdsAsync(21)).Should().BeEmpty();
    }

    [Fact]
    public async Task AdminUserService_Should_Only_Return_Assigned_Users()
    {
        await using var db = CreateDbContext();
        await SeedAsync(db);
        var store = new MemoryAssignmentStore();
        await store.ReplaceAsync(20, [10]);
        var service = new AdminUserService(new AdminUserRepository(db), null, store);

        var list = await service.GetUsersAsync(new AdminUserQuery(), 20, UserRole.Admin);
        var forbidden = await service.GetUserDetailAsync(11, 20, UserRole.Admin);

        list.Value!.Items.Select(item => item.UserId).Should().Equal(10u);
        forbidden.StatusCode.Should().Be(403);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new VocaNovaDbContext(options);
    }

    private static async Task SeedAsync(VocaNovaDbContext db)
    {
        db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = UserRole.User },
            new Role { RoleId = 2, RoleName = UserRole.Admin },
            new Role { RoleId = 3, RoleName = UserRole.SuperAdmin });
        db.Users.AddRange(
            NewUser(10, 1, "User A"),
            NewUser(11, 1, "User B"),
            NewUser(20, 2, "Admin A"),
            NewUser(21, 2, "Admin B"));
        await db.SaveChangesAsync();
    }

    private static User NewUser(uint id, uint roleId, string name) => new()
    {
        UserId = id,
        RoleId = roleId,
        Status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UserAuth = new UserAuth { UserId = id, GoogleEmail = $"{id}@example.com", UpdatedAt = DateTime.UtcNow },
        UserProfile = new UserProfile { UserId = id, FullName = name, UpdatedAt = DateTime.UtcNow },
    };

    private sealed class MemoryAssignmentStore : IAdminUserAssignmentStore
    {
        private readonly Dictionary<uint, IReadOnlyCollection<uint>> _items = [];

        public Task<IReadOnlyDictionary<uint, IReadOnlyCollection<uint>>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<uint, IReadOnlyCollection<uint>>>(_items);

        public Task<IReadOnlyCollection<uint>> GetUserIdsAsync(uint adminId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(adminId, out var ids) ? ids : (IReadOnlyCollection<uint>)[]);

        public Task ReplaceAsync(uint adminId, IReadOnlyCollection<uint> userIds, CancellationToken cancellationToken = default)
        {
            var selected = userIds.Distinct().ToArray();
            foreach (var key in _items.Keys.ToArray())
            {
                if (key == adminId) continue;
                _items[key] = _items[key].Except(selected).ToArray();
            }
            _items[adminId] = selected;
            return Task.CompletedTask;
        }
    }
}
