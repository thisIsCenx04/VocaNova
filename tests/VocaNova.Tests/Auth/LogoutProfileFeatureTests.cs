using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Repositories;
using VocaNova.API.Features.Auth.Services;
using VocaNova.API.Features.Auth.Validators;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using VocaNova.API.Infrastructure.Storage;

namespace VocaNova.Tests.Auth;

public class LogoutProfileFeatureTests
{
    private const string RawRefreshToken = "logout-refresh-token";

    [Fact]
    public async Task LogoutAsync_Should_Revoke_RefreshToken()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        await SeedRefreshTokenAsync(dbContext);
        var service = CreateAuthService(dbContext);

        var result = await service.LogoutAsync(new RefreshTokenRequest(RawRefreshToken));

        result.IsSuccess.Should().BeTrue();
        var tokenHash = TokenHelper.HashSha256(RawRefreshToken);
        var refreshToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash);
        refreshToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfileAsync_Should_Return_And_Cache_Profile()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var cache = new FakeUserProfileCache();
        var service = CreateAuthService(dbContext, cache);

        var result = await service.GetProfileAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(1);
        result.Value.Phone.Should().Be("0912345678");
        result.Value.FullName.Should().Be("Nguyen Van A");
        result.Value.LearningProfile.Should().NotBeNull();
        cache.SetCount.Should().Be(1);
        cache.StoredProfile.Should().BeEquivalentTo(result.Value);
    }

    [Fact]
    public async Task GetProfileAsync_Should_Return_Cached_Profile_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cachedProfile = new UserProfileDto(99, null, "Cached User", null, UserRole.User, UserStatus.Active, null);
        var cache = new FakeUserProfileCache(cachedProfile);
        var service = CreateAuthService(dbContext, cache);

        var result = await service.GetProfileAsync(99);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedProfile);
        cache.GetCount.Should().Be(1);
        (await dbContext.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateProfileAsync_Should_Update_Profile_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var cache = new FakeUserProfileCache();
        var service = CreateAuthService(dbContext, cache);

        var result = await service.UpdateProfileAsync(
            1,
            new UpdateUserProfileRequest("Tran Thi B", "https://example.com/avatar.png"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Tran Thi B");
        result.Value.AvatarUrl.Should().Be("https://example.com/avatar.png");
        cache.RemoveCount.Should().Be(1);

        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == 1);
        profile.FullName.Should().Be("Tran Thi B");
        profile.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public async Task UpdateLearningProfileAsync_Should_Upsert_Profile_When_ForeignKeys_Are_Valid()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, includeLearningProfile: false);
        await SeedLearningProfileLookupsAsync(dbContext);
        var cache = new FakeUserProfileCache();
        var knnCache = new FakeKnnTopicRecommendationCache();
        var service = CreateAuthService(dbContext, cache, knnCache);

        var result = await service.UpdateLearningProfileAsync(
            1,
            new UpdateLearningProfileRequest(1, 2, 3, 4, 5));

        result.IsSuccess.Should().BeTrue();
        result.Value!.LearningProfile.Should().NotBeNull();
        result.Value.LearningProfile!.AgeRangeId.Should().Be(1);
        result.Value.LearningProfile.RegionId.Should().Be(2);
        result.Value.LearningProfile.OccupationId.Should().Be(3);
        result.Value.LearningProfile.EducationLevelId.Should().Be(4);
        result.Value.LearningProfile.LearningPurposeId.Should().Be(5);
        cache.RemoveCount.Should().Be(1);

        var learningProfile = await dbContext.UserLearningProfiles.SingleAsync(profile => profile.UserId == 1);
        learningProfile.AgeRangeId.Should().Be(1);
        learningProfile.RegionId.Should().Be(2);
        learningProfile.OccupationId.Should().Be(3);
        learningProfile.EducationLevelId.Should().Be(4);
        learningProfile.LearningPurposeId.Should().Be(5);
        knnCache.RemoveCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateLearningProfileAsync_Should_Return_400_When_ForeignKey_Is_Invalid()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var service = CreateAuthService(dbContext);

        var result = await service.UpdateLearningProfileAsync(
            1,
            new UpdateLearningProfileRequest(999, null, null, null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Error.Should().Be("AgeRangeId is invalid.");
    }

    [Fact]
    public void UpdateUserProfileRequestValidator_Should_Reject_Invalid_AvatarUrl()
    {
        var validator = new UpdateUserProfileRequestValidator();

        var result = validator.Validate(new UpdateUserProfileRequest("Nguyen Van A", "not-a-url"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateUserProfileRequest.AvatarUrl));
    }

    [Fact]
    public async Task UploadAvatarAsync_Should_Upload_And_Update_Profile()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var cache = new FakeUserProfileCache();
        var storage = new FakeImageStorage("https://res.cloudinary.com/demo/avatar.png");
        var service = CreateAuthService(dbContext, cache, imageStorage: storage);
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(stream, 0, stream.Length, "file", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

        var result = await service.UploadAvatarAsync(1, new UploadAvatarRequest { File = file });

        result.IsSuccess.Should().BeTrue();
        result.Value!.AvatarUrl.Should().Be("https://res.cloudinary.com/demo/avatar.png");
        storage.Folder.Should().Be("vocanova/avatars");
        cache.RemoveCount.Should().Be(1);
        (await dbContext.UserProfiles.SingleAsync()).AvatarUrl.Should()
            .Be("https://res.cloudinary.com/demo/avatar.png");
    }

    [Fact]
    public async Task UploadAvatarAsync_Should_Reject_Unsupported_File()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var storage = new FakeImageStorage("https://res.cloudinary.com/demo/avatar.png");
        var service = CreateAuthService(dbContext, imageStorage: storage);
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(stream, 0, stream.Length, "file", "avatar.gif")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/gif",
        };

        var result = await service.UploadAvatarAsync(1, new UploadAvatarRequest { File = file });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        storage.UploadCount.Should().Be(0);
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static AuthService CreateAuthService(
        VocaNovaDbContext dbContext,
        IUserProfileCache? userProfileCache = null,
        IKnnTopicRecommendationCache? knnTopicRecommendationCache = null,
        IImageStorage? imageStorage = null)
    {
        return new AuthService(
            dbContext,
            new AuthRepository(dbContext),
            CreateJwtTokenService(),
            new FakeGoogleTokenVerifier(),
            Options.Create(CreateJwtSettings()),
            userProfileCache,
            knnTopicRecommendationCache: knnTopicRecommendationCache,
            imageStorage: imageStorage,
            cloudinarySettings: Options.Create(new CloudinarySettings
            {
                AvatarFolder = "vocanova/avatars",
            }));
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        return new JwtTokenService(Options.Create(CreateJwtSettings()));
    }

    private static JwtSettings CreateJwtSettings()
    {
        return new JwtSettings
        {
            Issuer = "VocaNova.Tests",
            Audience = "VocaNova.Tests.Clients",
            SecretKey = "THIS_IS_A_TEST_SECRET_KEY_WITH_32_CHARS_MIN",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        };
    }

    private static async Task SeedUserAsync(
        VocaNovaDbContext dbContext,
        bool includeLearningProfile = true)
    {
        var role = new Role
        {
            RoleId = 1,
            RoleName = UserRole.User,
        };

        var user = new User
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
                PasswordHash = PasswordHelper.Hash("Password1"),
                UpdatedAt = DateTime.UtcNow,
            },
            UserProfile = new UserProfile
            {
                UserId = 1,
                FullName = "Nguyen Van A",
                UpdatedAt = DateTime.UtcNow,
            },
        };

        if (includeLearningProfile)
        {
            user.UserLearningProfile = new UserLearningProfile
            {
                UserId = 1,
                AgeRangeId = 1,
                RegionId = 2,
                OccupationId = 3,
                EducationLevelId = 4,
                LearningPurposeId = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRefreshTokenAsync(VocaNovaDbContext dbContext)
    {
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1,
            TokenHash = TokenHelper.HashSha256(RawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedLearningProfileLookupsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.AgeRanges.Add(new AgeRange
        {
            AgeRangeId = 1,
            Name = "18-24",
            DisplayOrder = 1,
            Status = UserStatus.Active,
        });
        dbContext.Regions.Add(new Region
        {
            RegionId = 2,
            Name = "Ho Chi Minh",
            Code = "HCM",
            Status = UserStatus.Active,
        });
        dbContext.Occupations.Add(new Occupation
        {
            OccupationId = 3,
            Name = "Student",
            Status = UserStatus.Active,
        });
        dbContext.EducationLevels.Add(new EducationLevel
        {
            EducationLevelId = 4,
            Name = "University",
            DisplayOrder = 1,
            Status = UserStatus.Active,
        });
        dbContext.LearningPurposes.Add(new LearningPurpose
        {
            LearningPurposeId = 5,
            Name = "Work",
            Status = UserStatus.Active,
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        public Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<GoogleUserInfo?>(null);
        }
    }

    private sealed class FakeUserProfileCache : IUserProfileCache
    {
        private readonly UserProfileDto? _cachedProfile;

        public FakeUserProfileCache(UserProfileDto? cachedProfile = null)
        {
            _cachedProfile = cachedProfile;
        }

        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public UserProfileDto? StoredProfile { get; private set; }

        public Task<UserProfileDto?> GetAsync(uint userId, CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(_cachedProfile);
        }

        public Task SetAsync(UserProfileDto profile, CancellationToken cancellationToken = default)
        {
            SetCount++;
            StoredProfile = profile;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeKnnTopicRecommendationCache : IKnnTopicRecommendationCache
    {
        public int RemoveCount { get; private set; }

        public Task<IReadOnlyCollection<TopicRecommendationDto>?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<TopicRecommendationDto>?>(null);
        }

        public Task SetAsync(
            uint userId,
            IReadOnlyCollection<TopicRecommendationDto> recommendations,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeImageStorage : IImageStorage
    {
        private readonly string _url;

        public FakeImageStorage(string url)
        {
            _url = url;
        }

        public int UploadCount { get; private set; }

        public string? Folder { get; private set; }

        public Task<ImageStorageResult> UploadAsync(
            uint ownerId,
            IFormFile file,
            string? folder = null,
            CancellationToken cancellationToken = default)
        {
            UploadCount++;
            Folder = folder;
            return Task.FromResult(new ImageStorageResult($"{folder}/{ownerId}/avatar", _url));
        }
    }
}
