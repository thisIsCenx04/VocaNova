using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
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

public class OtpFeatureTests
{
    private const string Phone = "0912345678";
    private const string OtpCode = "123456";

    [Fact]
    public async Task SendOtpAsync_Should_Create_Active_Otp_And_Send_Sms()
    {
        await using var dbContext = CreateDbContext();
        var smsProvider = new FakeSmsProvider();
        var service = CreateAuthService(dbContext, smsProvider: smsProvider);

        var result = await service.SendOtpAsync(new OtpSendRequest(Phone, "register"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpiresIn.Should().Be(300);

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.Phone.Should().Be(Phone);
        otp.OtpCode.Should().Be(OtpCode);
        otp.IsUsed.Should().BeFalse();
        otp.Status.Should().Be(OtpStatus.Active);
        otp.VerifyAttemptCount.Should().Be(0);
        otp.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(4));

        smsProvider.Messages.Should().ContainSingle()
            .Which.Should().Be((Phone, OtpCode));
    }

    [Fact]
    public async Task SendOtpAsync_Should_Return_429_When_Phone_Is_Rate_Limited()
    {
        await using var dbContext = CreateDbContext();
        await SeedOtpAsync(dbContext, expiresAt: DateTime.UtcNow.AddMinutes(5), createdAt: DateTime.UtcNow);
        var service = CreateAuthService(dbContext);

        var result = await service.SendOtpAsync(new OtpSendRequest(Phone, "register"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        result.Error.Should().Be("OTP request rate limit exceeded.");
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Mark_Otp_Used_When_Code_Is_Valid()
    {
        await using var dbContext = CreateDbContext();
        await SeedOtpAsync(dbContext, expiresAt: DateTime.UtcNow.AddMinutes(5));
        var service = CreateAuthService(dbContext);

        var result = await service.VerifyOtpAsync(new OtpVerifyRequest(Phone, OtpCode));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Verified.Should().BeTrue();

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeTrue();
        otp.VerifyAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Return_401_When_Otp_Is_Expired()
    {
        await using var dbContext = CreateDbContext();
        await SeedOtpAsync(dbContext, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var service = CreateAuthService(dbContext);

        var result = await service.VerifyOtpAsync(new OtpVerifyRequest(Phone, OtpCode));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Error.Should().Be("OTP has expired.");

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeFalse();
        otp.VerifyAttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Return_429_When_Max_Attempts_Are_Exceeded()
    {
        await using var dbContext = CreateDbContext();
        await SeedOtpAsync(
            dbContext,
            expiresAt: DateTime.UtcNow.AddMinutes(5),
            verifyAttemptCount: AppSettings.OtpMaxVerifyAttempts);
        var service = CreateAuthService(dbContext);

        var result = await service.VerifyOtpAsync(new OtpVerifyRequest(Phone, OtpCode));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        result.Error.Should().Be("Maximum OTP verify attempts exceeded.");

        var otp = await dbContext.OtpVerifications.SingleAsync();
        otp.IsUsed.Should().BeFalse();
        otp.VerifyAttemptCount.Should().Be(AppSettings.OtpMaxVerifyAttempts);
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Return_409_When_Otp_Is_Already_Used()
    {
        await using var dbContext = CreateDbContext();
        await SeedOtpAsync(
            dbContext,
            expiresAt: DateTime.UtcNow.AddMinutes(5),
            isUsed: true);
        var service = CreateAuthService(dbContext);

        var result = await service.VerifyOtpAsync(new OtpVerifyRequest(Phone, OtpCode));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        result.Error.Should().Be("OTP has already been used.");
    }

    [Fact]
    public void OtpVerifyRequestValidator_Should_Reject_NonNumeric_Code()
    {
        var validator = new OtpVerifyRequestValidator();

        var result = validator.Validate(new OtpVerifyRequest(Phone, "abcdef"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(OtpVerifyRequest.OtpCode));
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

    private static async Task SeedOtpAsync(
        VocaNovaDbContext dbContext,
        DateTime expiresAt,
        DateTime? createdAt = null,
        bool isUsed = false,
        int verifyAttemptCount = 0)
    {
        dbContext.OtpVerifications.Add(new OtpVerification
        {
            Phone = Phone,
            OtpCode = OtpCode,
            IsUsed = isUsed,
            Status = OtpStatus.Active,
            VerifyAttemptCount = verifyAttemptCount,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt ?? DateTime.UtcNow.AddMinutes(-2),
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
