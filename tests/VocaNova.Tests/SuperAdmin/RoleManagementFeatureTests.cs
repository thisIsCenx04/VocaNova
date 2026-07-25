using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.SuperAdmin.DTOs;
using VocaNova.API.Features.SuperAdmin.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.SuperAdmin;

public sealed class RoleManagementFeatureTests
{
    [Fact]
    public async Task GetRolesAsync_Should_Return_RoleId_And_RoleName()
    {
        await using var db = CreateDbContext();
        await SeedAsync(db);
        var service = new RoleManagementService(db);

        var result = await service.GetRolesAsync(new RoleQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items.Should().Contain(role => role.RoleId == 1 && role.RoleName == UserRole.User);
    }

    [Fact]
    public async Task RoleManagement_Should_Create_And_Assign_Custom_Role()
    {
        await using var db = CreateDbContext();
        await SeedAsync(db);
        var service = new RoleManagementService(db);

        var created = await service.CreateAsync(new SaveRoleRequest("support_agent"));
        var assigned = await service.AssignRoleAsync(created.Value!.RoleId, 10);

        created.IsSuccess.Should().BeTrue();
        assigned.IsSuccess.Should().BeTrue();
        (await db.Users.FindAsync(10u))!.RoleId.Should().Be(created.Value.RoleId);
    }

    [Fact]
    public async Task Delete_Should_Reject_System_And_InUse_Roles()
    {
        await using var db = CreateDbContext();
        await SeedAsync(db);
        var service = new RoleManagementService(db);
        var custom = await service.CreateAsync(new SaveRoleRequest("support_agent"));
        await service.AssignRoleAsync(custom.Value!.RoleId, 10);

        var systemResult = await service.DeleteAsync(1);
        var inUseResult = await service.DeleteAsync(custom.Value.RoleId);

        systemResult.StatusCode.Should().Be(403);
        inUseResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task RemoveRole_Should_Return_Account_To_Default_User_Role()
    {
        await using var db = CreateDbContext();
        await SeedAsync(db);
        var service = new RoleManagementService(db);
        var custom = await service.CreateAsync(new SaveRoleRequest("support_agent"));
        await service.AssignRoleAsync(custom.Value!.RoleId, 10);

        var result = await service.RemoveRoleAsync(custom.Value.RoleId, 10);

        result.IsSuccess.Should().BeTrue();
        (await db.Users.FindAsync(10u))!.RoleId.Should().Be(1);
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
        db.Users.Add(NewUser(10, 1, "User A"));
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
}
