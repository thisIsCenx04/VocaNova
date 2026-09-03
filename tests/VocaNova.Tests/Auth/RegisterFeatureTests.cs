using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.Contracts.Requests;
using VocaNova.API.Features.Auth.Contracts.Responses;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.DAL.Repositories;
using VocaNova.API.Features.Auth.BLL.Services;
using VocaNova.API.Features.Auth.Contracts.Requests;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Auth;

public class RegisterFeatureTests
{
    [Fact]
    public async Task RegisterAsync_Should_Create_User_Auth_Profile_RefreshToken_And_Return_Tokens()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedOtpAsync(dbContext, "0912345678", "123456", DateTime.UtcNow.AddMinutes(5));
        var service = CreateAuthService(dbContext);

        var result = await service.RegisterAsync(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "123456"),
            deviceInfo: "xunit",
            ipAddress: "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.ExpiresIn.Should().Be(900);

        var user = await dbContext.Users
            .Include(entity => entity.UserAuth)
            .Include(entity => entity.UserProfile)
            .SingleAsync();
        user.Status.Should().Be(UserStatus.Active);
        user.RoleId.Should().Be(1);
        user.UserAuth!.Phone.Should().Be("0912345678");
        user.UserAuth.IsPhoneVerified.Should().BeTrue();
        PasswordHelper.Verify("Password1", user.UserAuth.PasswordHash!).Should().BeTrue();
        user.UserProfile!.FullName.Should().Be("Nguyen Van A");

        var refreshToken = await dbContext.RefreshTokens.SingleAsync();
        refreshToken.UserId.Should().Be(user.UserId);
        refreshToken.TokenHash.Should().Be(TokenHelper.HashSha256(result.Value.RefreshToken));
        refreshToken.DeviceInfo.Should().Be("xunit");
        refreshToken.IpAddress.Should().Be("127.0.0.1");

        var principal = CreateJwtTokenService().ValidateAccessToken(result.Value.AccessToken);
        principal.Should().NotBeNull();
        principal!.FindFirst("user_id")!.Value.Should().Be(user.UserId.ToString());
        principal.FindFirst("role")!.Value.Should().Be(UserRole.User);

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeTrue();
        otp.UserId.Should().Be(user.UserId);
        otp.VerifyAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_Should_Return_Conflict_When_Active_Phone_Already_Exists()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedExistingUserAsync(dbContext, UserStatus.Active);
        var service = CreateAuthService(dbContext);

        var result = await service.RegisterAsync(new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "123456"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        result.Error.Should().Be("Phone already exists.");
        (await dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_Should_Return_401_And_Not_Create_User_When_Otp_Is_Invalid()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedOtpAsync(dbContext, "0912345678", "123456", DateTime.UtcNow.AddMinutes(5));
        var service = CreateAuthService(dbContext);

        var result = await service.RegisterAsync(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "654321"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        (await dbContext.Users.CountAsync()).Should().Be(0);

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeFalse();
        otp.VerifyAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_Should_Not_Accept_Reset_Otp()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedOtpAsync(dbContext, "0912345678", "123456", DateTime.UtcNow.AddMinutes(5), userId: 99);
        var service = CreateAuthService(dbContext);

        var result = await service.RegisterAsync(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "123456"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        (await dbContext.Users.CountAsync()).Should().Be(0);

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeFalse();
        otp.VerifyAttemptCount.Should().Be(0);
    }

    [Fact]
    public void RegisterRequestValidator_Should_Reject_Weak_Password()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest("0912345678", "weak", "Nguyen Van A", "123456"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public async Task RegisterAsync_Should_Seed_Learning_Profile_And_Derive_AgeRange_From_DateOfBirth()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedLearningProfileLookupsAsync(dbContext);
        await SeedOtpAsync(dbContext, "0912345678", "123456", DateTime.UtcNow.AddMinutes(5));
        var service = CreateAuthService(dbContext);
        var dateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-20);

        var result = await service.RegisterAsync(new RegisterRequest(
            "0912345678",
            "Password1",
            "Nguyen Van A",
            "123456",
            DateOfBirth: dateOfBirth,
            RegionId: 1,
            OccupationId: 1,
            EducationLevelId: 1));

        result.IsSuccess.Should().BeTrue();
        var profile = await dbContext.UserLearningProfiles.SingleAsync();
        // The schema has no date-of-birth column, so only the bucket the age falls into is kept.
        profile.AgeRangeId.Should().Be(2);
        profile.RegionId.Should().Be(1);
        profile.OccupationId.Should().Be(1);
        profile.EducationLevelId.Should().Be(1);
        profile.LearningPurposeId.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_Should_Skip_Learning_Profile_When_No_Optional_Fields_Are_Supplied()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedOtpAsync(dbContext, "0912345678", "123456", DateTime.UtcNow.AddMinutes(5));
        var service = CreateAuthService(dbContext);

        var result = await service.RegisterAsync(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "123456"));

        result.IsSuccess.Should().BeTrue();
        (await dbContext.UserLearningProfiles.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegisterAsync_Should_Reject_Unknown_Lookup_Reference()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserRoleAsync(dbContext);
        await SeedLearningProfileLookupsAsync(dbContext);
        await SeedOtpAsync(dbContext, "0912345678", "123456", DateTime.UtcNow.AddMinutes(5));
        var service = CreateAuthService(dbContext);

        var result = await service.RegisterAsync(new RegisterRequest(
            "0912345678",
            "Password1",
            "Nguyen Van A",
            "123456",
            RegionId: 999));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"{nameof(RegisterRequest.RegionId)} is invalid.");
        (await dbContext.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void RegisterRequestValidator_Should_Reject_Implausible_DateOfBirth()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest(
            "0912345678",
            "Password1",
            "Nguyen Van A",
            "123456",
            DateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName.Contains(nameof(RegisterRequest.DateOfBirth)));
    }

    [Fact]
    public void RegisterRequestValidator_Should_Accept_Request_Without_Optional_Profile_Fields()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "123456"));

        result.IsValid.Should().BeTrue();
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static AuthService CreateAuthService(VocaNovaDbContext dbContext)
    {
        return AuthTestFactory.CreateService(dbContext, googleIdentityProvider: new FakeGoogleTokenVerifier(null));
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

    private static async Task SeedUserRoleAsync(VocaNovaDbContext dbContext)
    {
        dbContext.Roles.Add(new Role
        {
            RoleId = 1,
            RoleName = UserRole.User,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedLearningProfileLookupsAsync(VocaNovaDbContext dbContext)
    {
        dbContext.AgeRanges.AddRange(
            new AgeRange
            {
                AgeRangeId = 1,
                Name = "Under 18",
                MinAge = null,
                MaxAge = 17,
                DisplayOrder = 1,
                Status = UserStatus.Active,
            },
            new AgeRange
            {
                AgeRangeId = 2,
                Name = "18-24",
                MinAge = 18,
                MaxAge = 24,
                DisplayOrder = 2,
                Status = UserStatus.Active,
            },
            new AgeRange
            {
                AgeRangeId = 3,
                Name = "25+",
                MinAge = 25,
                MaxAge = null,
                DisplayOrder = 3,
                Status = UserStatus.Active,
            });
        dbContext.Regions.Add(new Region
        {
            RegionId = 1,
            Name = "Mien Bac",
            Code = "MB",
            Status = UserStatus.Active,
        });
        dbContext.Occupations.Add(new Occupation
        {
            OccupationId = 1,
            Name = "Student",
            Status = UserStatus.Active,
        });
        dbContext.EducationLevels.Add(new EducationLevel
        {
            EducationLevelId = 1,
            Name = "University",
            DisplayOrder = 1,
            Status = UserStatus.Active,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedOtpAsync(
        VocaNovaDbContext dbContext,
        string phone,
        string otpCode,
        DateTime expiresAt,
        uint? userId = null)
    {
        dbContext.OtpVerifications.Add(new OtpVerification
        {
            UserId = userId,
            Phone = phone,
            OtpCode = otpCode,
            IsUsed = false,
            Status = OtpStatus.Active,
            VerifyAttemptCount = 0,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedExistingUserAsync(VocaNovaDbContext dbContext, string status)
    {
        dbContext.Users.Add(new User
        {
            UserId = 100,
            RoleId = 1,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserAuth = new UserAuth
            {
                UserId = 100,
                Phone = "0912345678",
                PasswordHash = PasswordHelper.Hash("Password1"),
                UpdatedAt = DateTime.UtcNow,
            },
            UserProfile = new EntityUserProfile
            {
                UserId = 100,
                FullName = "Existing User",
                UpdatedAt = DateTime.UtcNow,
            },
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        private readonly GoogleUserInfo? _userInfo;

        public FakeGoogleTokenVerifier(GoogleUserInfo? userInfo)
        {
            _userInfo = userInfo;
        }

        public Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_userInfo);
        }
    }
}
