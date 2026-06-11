using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Auth.Repositories;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Auth;

public class AuthRepositoryTests
{
    [Fact]
    public async Task FindByPhoneAsync_Should_Return_User_Aggregate()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var repository = new AuthRepository(dbContext);

        var user = await repository.FindByPhoneAsync("0912345678");

        user.Should().NotBeNull();
        user!.UserAuth.Should().NotBeNull();
        user.UserAuth!.Phone.Should().Be("0912345678");
        user.UserProfile.Should().NotBeNull();
        user.Role.RoleName.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task FindByGoogleUidAsync_Should_Return_User_Aggregate()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var repository = new AuthRepository(dbContext);

        var user = await repository.FindByGoogleUidAsync("google-uid-1");

        user.Should().NotBeNull();
        user!.UserAuth!.GoogleUid.Should().Be("google-uid-1");
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_And_RevokeTokenAsync_Should_Persist_Token_State()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var repository = new AuthRepository(dbContext);

        await repository.CreateRefreshTokenAsync(new RefreshToken
        {
            UserId = 1,
            TokenHash = "token-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
        });

        var revokedAt = DateTime.UtcNow;
        var revoked = await repository.RevokeTokenAsync("token-hash", revokedAt);

        revoked.Should().BeTrue();
        var token = await dbContext.RefreshTokens.SingleAsync(refreshToken => refreshToken.TokenHash == "token-hash");
        token.RevokedAt.Should().Be(revokedAt);
    }

    [Fact]
    public async Task CreateUserAsync_Should_Persist_User_Auth_Profile_And_Learning_Profile()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Roles.Add(new Role
        {
            RoleId = 1,
            RoleName = UserRole.User,
        });
        await dbContext.SaveChangesAsync();
        var repository = new AuthRepository(dbContext);

        var user = await repository.CreateUserAsync(
            new User
            {
                RoleId = 1,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new UserAuth
            {
                Phone = "0987654321",
                PasswordHash = "hash",
                UpdatedAt = DateTime.UtcNow,
            },
            new UserProfile
            {
                FullName = "Tran Thi B",
                UpdatedAt = DateTime.UtcNow,
            },
            new UserLearningProfile
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        user.UserId.Should().BeGreaterThan(0);
        (await dbContext.UserAuths.FindAsync(user.UserId)).Should().NotBeNull();
        (await dbContext.UserProfiles.FindAsync(user.UserId)).Should().NotBeNull();
        (await dbContext.UserLearningProfiles.FindAsync(user.UserId)).Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeTokenAsync_Should_Return_False_When_Token_Does_Not_Exist()
    {
        await using var dbContext = CreateDbContext();
        var repository = new AuthRepository(dbContext);

        var revoked = await repository.RevokeTokenAsync("missing", DateTime.UtcNow);

        revoked.Should().BeFalse();
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static async Task SeedUserAsync(VocaNovaDbContext dbContext)
    {
        var role = new Role
        {
            RoleId = 1,
            RoleName = UserRole.User,
        };

        dbContext.Roles.Add(role);
        dbContext.Users.Add(new User
        {
            UserId = 1,
            RoleId = role.RoleId,
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserAuth = new UserAuth
            {
                UserId = 1,
                Phone = "0912345678",
                GoogleUid = "google-uid-1",
                GoogleEmail = "user@example.com",
                PasswordHash = "hash",
                UpdatedAt = DateTime.UtcNow,
            },
            UserProfile = new UserProfile
            {
                UserId = 1,
                FullName = "Nguyen Van A",
                UpdatedAt = DateTime.UtcNow,
            },
            UserLearningProfile = new UserLearningProfile
            {
                UserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        });

        await dbContext.SaveChangesAsync();
    }
}
