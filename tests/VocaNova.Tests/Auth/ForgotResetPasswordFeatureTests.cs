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
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Otp;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using VocaNova.API.Infrastructure.RateLimiting;
using VocaNova.API.Infrastructure.Sms;

namespace VocaNova.Tests.Auth;

public class ForgotResetPasswordFeatureTests
{
    private const string Phone = "0912345678";
    private const string OtpCode = "123456";

    [Fact]
    public async Task ForgotPasswordAsync_Should_Send_Reset_Otp_For_Existing_User()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var smsProvider = new FakeSmsProvider();
        var service = CreateAuthService(dbContext, smsProvider);

        var result = await service.ForgotPasswordAsync(new ForgotPasswordRequest(Phone));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpiresIn.Should().Be(300);

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.Phone.Should().Be(Phone);
        otp.UserId.Should().Be(1);
        otp.OtpCode.Should().Be(OtpCode);
        otp.IsUsed.Should().BeFalse();
        smsProvider.Messages.Should().ContainSingle()
            .Which.Should().Be((Phone, OtpCode));
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_Update_Password_And_Mark_Otp_Used_When_Otp_Is_Valid()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        await SeedOtpAsync(dbContext, DateTime.UtcNow.AddMinutes(5));
        var service = CreateAuthService(dbContext);

        var result = await service.ResetPasswordAsync(
            new ResetPasswordRequest(Phone, OtpCode, "NewPassword1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var userAuth = await dbContext.UserAuths.SingleAsync(auth => auth.UserId == 1);
        PasswordHelper.Verify("NewPassword1", userAuth.PasswordHash!).Should().BeTrue();
        PasswordHelper.Verify("OldPassword1", userAuth.PasswordHash!).Should().BeFalse();

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeTrue();
        otp.VerifyAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_Return_401_And_Not_Update_Password_When_Otp_Is_Expired()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        await SeedOtpAsync(dbContext, DateTime.UtcNow.AddMinutes(-1));
        var service = CreateAuthService(dbContext);

        var result = await service.ResetPasswordAsync(
            new ResetPasswordRequest(Phone, OtpCode, "NewPassword1"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Error.Should().Be("OTP has expired.");

        var userAuth = await dbContext.UserAuths.SingleAsync(auth => auth.UserId == 1);
        PasswordHelper.Verify("OldPassword1", userAuth.PasswordHash!).Should().BeTrue();

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeFalse();
        otp.VerifyAttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_Not_Accept_Register_Otp()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        await SeedOtpAsync(dbContext, DateTime.UtcNow.AddMinutes(5), userId: null);
        var service = CreateAuthService(dbContext);

        var result = await service.ResetPasswordAsync(
            new ResetPasswordRequest(Phone, OtpCode, "NewPassword1"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var userAuth = await dbContext.UserAuths.SingleAsync(auth => auth.UserId == 1);
        PasswordHelper.Verify("OldPassword1", userAuth.PasswordHash!).Should().BeTrue();

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeFalse();
        otp.VerifyAttemptCount.Should().Be(0);
    }

    [Fact]
    public void ResetPasswordRequestValidator_Should_Reject_Weak_NewPassword()
    {
        var validator = new ResetPasswordRequestValidator();

        var result = validator.Validate(new ResetPasswordRequest(Phone, OtpCode, "weak"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ResetPasswordRequest.NewPassword));
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
        ISmsProvider? smsProvider = null)
    {
        return new AuthService(
            dbContext,
            new AuthRepository(dbContext),
            CreateJwtTokenService(),
            new FakeGoogleTokenVerifier(),
            Options.Create(CreateJwtSettings()),
            otpCodeGenerator: new FixedOtpCodeGenerator(),
            smsProvider: smsProvider ?? new FakeSmsProvider(),
            rateLimitSettings: Options.Create(new RateLimitSettings()));
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
                Phone = Phone,
                PasswordHash = PasswordHelper.Hash("OldPassword1"),
                UpdatedAt = DateTime.UtcNow,
            },
            UserProfile = new UserProfile
            {
                UserId = 1,
                FullName = "Nguyen Van A",
                UpdatedAt = DateTime.UtcNow,
            },
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedOtpAsync(
        VocaNovaDbContext dbContext,
        DateTime expiresAt,
        uint? userId = 1)
    {
        dbContext.OtpVerifications.Add(new OtpVerification
        {
            UserId = userId,
            Phone = Phone,
            OtpCode = OtpCode,
            IsUsed = false,
            Status = OtpStatus.Active,
            VerifyAttemptCount = 0,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FixedOtpCodeGenerator : IOtpCodeGenerator
    {
        public string Generate()
        {
            return OtpCode;
        }
    }

    private sealed class FakeSmsProvider : ISmsProvider
    {
        public List<(string Phone, string OtpCode)> Messages { get; } = new();

        public Task SendOtpAsync(string phone, string otpCode, CancellationToken cancellationToken = default)
        {
            Messages.Add((phone, otpCode));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        public Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<GoogleUserInfo?>(null);
        }
    }
}
