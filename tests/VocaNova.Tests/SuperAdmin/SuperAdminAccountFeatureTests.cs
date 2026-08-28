using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.SuperAdmin.Contracts.Requests;
using VocaNova.API.Features.SuperAdmin.Contracts.Responses;
using VocaNova.API.Features.SuperAdmin.BLL.Models;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Services;
using VocaNova.API.Features.SuperAdmin.DAL.Repositories;
using VocaNova.API.Features.SuperAdmin.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using VocaNova.API.Infrastructure.Persistence.Transactions;

namespace VocaNova.Tests.SuperAdmin;

public sealed class SuperAdminAccountFeatureTests
{
    [Fact]
    public async Task CreateAsync_Should_Create_LoginReady_Admin_With_Hashed_Password()
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateAdminAccountRequest(
            "New Admin", "NEW.ADMIN@EXAMPLE.COM", "0934567890", "Strong123").ToModel());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(UserRole.Admin);
        result.Value.Email.Should().Be("new.admin@example.com");
        var created = await dbContext.Users
            .Include(user => user.UserAuth)
            .Include(user => user.UserProfile)
            .Include(user => user.Role)
            .SingleAsync(user => user.UserId == result.Value.AdminId);
        created.Role.RoleName.Should().Be(UserRole.Admin);
        created.UserProfile!.FullName.Should().Be("New Admin");
        created.UserAuth!.IsPhoneVerified.Should().BeTrue();
        created.UserAuth.PasswordHash.Should().NotBe("Strong123");
        PasswordHelper.Verify("Strong123", created.UserAuth.PasswordHash!).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_Phone_Or_Email()
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var duplicatePhone = await service.CreateAsync(new CreateAdminAccountRequest(
            "New Admin", "unique@example.com", "0912345678", "Strong123").ToModel());
        var duplicateEmail = await service.CreateAsync(new CreateAdminAccountRequest(
            "New Admin", "ADMIN@EXAMPLE.COM", "0934567890", "Strong123").ToModel());

        duplicatePhone.StatusCode.Should().Be(409);
        duplicatePhone.Error.Should().Be("Phone already exists.");
        duplicateEmail.StatusCode.Should().Be(409);
        duplicateEmail.Error.Should().Be("Email already exists.");
    }

    [Fact]
    public async Task GetAccountsAsync_Should_Return_Only_Admin_Role()
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetAccountsAsync(new AdminAccountQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items.Single().AdminId.Should().Be(2);
    }

    [Fact]
    public async Task GetAccountsAsync_Should_Hide_Deleted_Admins_Unless_Requested()
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        var deletedAdmin = await dbContext.Users.SingleAsync(user => user.UserId == 2);
        deletedAdmin.Status = UserStatus.Deleted;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var hidden = await service.GetAccountsAsync(new AdminAccountQuery());
        var included = await service.GetAccountsAsync(new AdminAccountQuery(IncludeDeleted: true));

        hidden.Value!.Items.Should().BeEmpty();
        included.Value!.Items.Should().ContainSingle(item => item.AdminId == 2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Mutations_Should_Not_Manage_User_Or_SuperAdmin(uint targetId)
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var lockResult = await service.LockAsync(targetId);
        var deleteResult = await service.DeleteAsync(targetId);

        lockResult.StatusCode.Should().Be(404);
        deleteResult.StatusCode.Should().Be(404);
        (await dbContext.Users.SingleAsync(user => user.UserId == targetId)).Status
            .Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task LockAndUnlockAsync_Should_Change_Status_And_Revoke_Active_Tokens()
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenId = 1,
            UserId = 2,
            TokenHash = "admin-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var locked = await service.LockAsync(2);
        var unlocked = await service.UnlockAsync(2);

        locked.IsSuccess.Should().BeTrue();
        unlocked.IsSuccess.Should().BeTrue();
        (await dbContext.Users.SingleAsync(user => user.UserId == 2)).Status.Should().Be(UserStatus.Active);
        (await dbContext.RefreshTokens.SingleAsync()).RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Profile_And_Revoke_Token_When_Password_Changes()
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenId = 1,
            UserId = 2,
            TokenHash = "admin-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(2, new UpdateAdminAccountRequest(
            "Updated Admin", "updated@example.com", "0976543210", "Updated123").ToModel());

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Updated Admin");
        var auth = await dbContext.UserAuths.SingleAsync(item => item.UserId == 2);
        PasswordHelper.Verify("Updated123", auth.PasswordHash!).Should().BeTrue();
        (await dbContext.RefreshTokens.SingleAsync()).RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Clear_Credentials_And_Revoke_Tokens()
    {
        await using var dbContext = CreateDbContext();
        await SeedRolesAndUsersAsync(dbContext);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenId = 1,
            UserId = 2,
            TokenHash = "admin-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(2);

        result.IsSuccess.Should().BeTrue();
        var admin = await dbContext.Users.Include(user => user.UserAuth).SingleAsync(user => user.UserId == 2);
        admin.Status.Should().Be(UserStatus.Deleted);
        admin.UserAuth!.Phone.Should().BeNull();
        admin.UserAuth.GoogleEmail.Should().BeNull();
        admin.UserAuth.PasswordHash.Should().BeNull();
        (await dbContext.RefreshTokens.SingleAsync()).RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void Validators_Should_Reject_Invalid_Account_Inputs()
    {
        var createValidator = new CreateAdminAccountRequestValidator();
        var updateValidator = new UpdateAdminAccountRequestValidator();

        var createResult = createValidator.TestValidate(new CreateAdminAccountRequest(
            "A", "invalid", "123", "weak", UserStatus.Deleted));
        var updateResult = updateValidator.TestValidate(new UpdateAdminAccountRequest(
            "", "invalid", "123", "weak", UserStatus.Deleted));

        createResult.ShouldHaveAnyValidationError();
        updateResult.ShouldHaveAnyValidationError();
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new VocaNovaDbContext(options);
    }

    private static SuperAdminAccountService CreateService(VocaNovaDbContext dbContext) =>
        new(
            new SuperAdminAccountRepository(dbContext),
            new EfApplicationTransactionManager(dbContext));

    private static async Task SeedRolesAndUsersAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Roles.AddRange(
            new Role { RoleId = 1, RoleName = UserRole.User },
            new Role { RoleId = 2, RoleName = UserRole.Admin },
            new Role { RoleId = 3, RoleName = UserRole.SuperAdmin });
        AddUser(dbContext, 1, 1, "Normal User", "normal@example.com", "0901234567");
        AddUser(dbContext, 2, 2, "Existing Admin", "admin@example.com", "0912345678");
        AddUser(dbContext, 3, 3, "Root Admin", "root@example.com", "0923456789");
        await dbContext.SaveChangesAsync();
    }

    private static void AddUser(
        VocaNovaDbContext dbContext,
        uint userId,
        uint roleId,
        string fullName,
        string email,
        string phone)
    {
        var now = DateTime.UtcNow;
        dbContext.Users.Add(new User
        {
            UserId = userId,
            RoleId = roleId,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            UserAuth = new UserAuth
            {
                UserId = userId,
                GoogleEmail = email,
                Phone = phone,
                PasswordHash = PasswordHelper.Hash("Original123"),
                IsPhoneVerified = true,
                UpdatedAt = now,
            },
            UserProfile = new EntityUserProfile
            {
                UserId = userId,
                FullName = fullName,
                UpdatedAt = now,
            },
        });
    }
}
