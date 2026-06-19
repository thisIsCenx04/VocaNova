using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Auth.Controllers;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Services;
using VocaNova.API.Infrastructure.RateLimiting;

namespace VocaNova.Tests.Auth;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_Should_Return_201_When_Service_Succeeds()
    {
        var controller = CreateController(new StubAuthService(
            Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900))));

        var result = await controller.Register(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "123456"),
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().BeAssignableTo<ApiResponse<TokenResponse>>();
    }

    [Fact]
    public async Task Register_Should_Return_409_When_Phone_Already_Exists()
    {
        var controller = CreateController(new StubAuthService(
            Result<TokenResponse>.Conflict("Phone already exists.")));

        var result = await controller.Register(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A", "123456"),
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Login_Should_Return_200_When_Service_Succeeds()
    {
        var controller = CreateController(new StubAuthService(
            Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900))));

        var result = await controller.Login(
            new LoginRequest("0912345678", "Password1"),
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        objectResult.Value.Should().BeAssignableTo<ApiResponse<TokenResponse>>();
    }

    [Fact]
    public async Task Login_Should_Return_Forbidden_When_Service_Returns_Forbidden()
    {
        var controller = CreateController(new StubAuthService(
            Result<TokenResponse>.Forbidden("User account is locked.")));

        var result = await controller.Login(
            new LoginRequest("0912345678", "Password1"),
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Login_Should_Return_429_With_RetryAfter_When_Ip_Rate_Limit_Is_Exceeded()
    {
        var authService = new StubAuthService(
            Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900)));
        var controller = CreateController(
            authService,
            new InMemoryAuthRateLimiter(),
            new RateLimitSettings
            {
                LoginPerMinutePerIp = 10,
                OtpPerMinutePerIp = 1,
                RetryAfterSeconds = 60,
            });
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        for (var index = 0; index < 10; index++)
        {
            var allowedResult = await controller.Login(
                new LoginRequest("0912345678", "Password1"),
                CancellationToken.None);

            allowedResult.Should().BeOfType<OkObjectResult>();
        }

        var blockedResult = await controller.Login(
            new LoginRequest("0912345678", "Password1"),
            CancellationToken.None);

        var objectResult = blockedResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        controller.Response.Headers["Retry-After"].ToString().Should().NotBeNullOrWhiteSpace();
        authService.LoginCallCount.Should().Be(10);
    }

    [Fact]
    public async Task GoogleLogin_Should_Return_200_When_Service_Succeeds()
    {
        var controller = CreateController(new StubAuthService(
            Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900))));

        var result = await controller.GoogleLogin(
            new GoogleLoginRequest("google-id-token"),
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        objectResult.Value.Should().BeAssignableTo<ApiResponse<TokenResponse>>();
    }

    [Fact]
    public async Task GoogleLogin_Should_Return_429_With_RetryAfter_When_Ip_Rate_Limit_Is_Exceeded()
    {
        var authService = new StubAuthService(
            Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900)));
        var controller = CreateController(
            authService,
            new InMemoryAuthRateLimiter(),
            new RateLimitSettings
            {
                LoginPerMinutePerIp = 10,
                OtpPerMinutePerIp = 1,
                RetryAfterSeconds = 60,
            });
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.3");

        for (var index = 0; index < 10; index++)
        {
            var allowedResult = await controller.GoogleLogin(
                new GoogleLoginRequest("google-id-token"),
                CancellationToken.None);

            allowedResult.Should().BeOfType<OkObjectResult>();
        }

        var blockedResult = await controller.GoogleLogin(
            new GoogleLoginRequest("google-id-token"),
            CancellationToken.None);

        var objectResult = blockedResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        controller.Response.Headers["Retry-After"].ToString().Should().NotBeNullOrWhiteSpace();
        authService.GoogleLoginCallCount.Should().Be(10);
    }

    [Fact]
    public async Task RefreshToken_Should_Return_200_When_Service_Succeeds()
    {
        var controller = CreateController(new StubAuthService(
            Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900))));

        var result = await controller.RefreshToken(
            new RefreshTokenRequest("refresh-token"),
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        objectResult.Value.Should().BeAssignableTo<ApiResponse<TokenResponse>>();
    }

    [Fact]
    public async Task GetMe_Should_Return_200_When_UserId_Claim_Is_Present()
    {
        var profile = new UserProfileDto(1, "0912345678", "Nguyen Van A", null, "user", "active", null);
        var controller = CreateController(new StubAuthService(
            profileResult: Result<UserProfileDto>.Ok(profile)));
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("user_id", "1") },
            "Test"));

        var result = await controller.GetMe(CancellationToken.None);

        var objectResult = result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        objectResult.Value.Should().BeAssignableTo<ApiResponse<UserProfileDto>>();
    }

    [Fact]
    public async Task SendOtp_Should_Return_429_With_RetryAfter_When_Ip_Rate_Limit_Is_Exceeded()
    {
        var controller = CreateController(
            new StubAuthService(),
            new InMemoryAuthRateLimiter(),
            new RateLimitSettings
            {
                LoginPerMinutePerIp = 10,
                OtpPerMinutePerIp = 1,
                RetryAfterSeconds = 60,
            });
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.2");

        var firstResult = await controller.SendOtp(
            new OtpSendRequest("0912345678", "reset"),
            CancellationToken.None);
        var secondResult = await controller.SendOtp(
            new OtpSendRequest("0912345678", "reset"),
            CancellationToken.None);

        firstResult.Should().BeOfType<OkObjectResult>();
        var objectResult = secondResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        controller.Response.Headers["Retry-After"].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RateLimitSettings_Should_Keep_Defaults_When_Config_Binds_Zero_Values()
    {
        var settings = new RateLimitSettings
        {
            OtpPerMinutePerPhone = 0,
            OtpPerMinutePerIp = 0,
            LoginPerMinutePerIp = 0,
            RetryAfterSeconds = 0,
        };

        settings.OtpPerMinutePerPhone.Should().Be(1);
        settings.OtpPerMinutePerIp.Should().Be(1);
        settings.LoginPerMinutePerIp.Should().Be(10);
        settings.RetryAfterSeconds.Should().Be(60);
    }

    private static AuthController CreateController(
        IAuthService authService,
        IAuthRateLimiter? authRateLimiter = null,
        RateLimitSettings? rateLimitSettings = null)
    {
        return new AuthController(
            authService,
            authRateLimiter,
            Options.Create(rateLimitSettings ?? new RateLimitSettings()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
    }

    private sealed class StubAuthService : IAuthService
    {
        private readonly Result<TokenResponse> _result;
        private readonly Result<UserProfileDto> _profileResult;
        private readonly Result<bool> _logoutResult;

        public StubAuthService(
            Result<TokenResponse>? result = null,
            Result<UserProfileDto>? profileResult = null,
            Result<bool>? logoutResult = null)
        {
            _result = result ?? Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900));
            _profileResult = profileResult ?? Result<UserProfileDto>.Ok(
                new UserProfileDto(1, "0912345678", "Nguyen Van A", null, "user", "active", null));
            _logoutResult = logoutResult ?? Result<bool>.Ok(true);
        }

        public int LoginCallCount { get; private set; }

        public int GoogleLoginCallCount { get; private set; }

        public Task<Result<TokenResponse>> RegisterAsync(
            RegisterRequest request,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }

        public Task<Result<TokenResponse>> LoginAsync(
            LoginRequest request,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            LoginCallCount++;
            return Task.FromResult(_result);
        }

        public Task<Result<TokenResponse>> GoogleLoginAsync(
            GoogleLoginRequest request,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            GoogleLoginCallCount++;
            return Task.FromResult(_result);
        }

        public Task<Result<TokenResponse>> RefreshTokenAsync(
            RefreshTokenRequest request,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }

        public Task<Result<bool>> LogoutAsync(
            RefreshTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_logoutResult);
        }

        public Task<Result<UserProfileDto>> GetProfileAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profileResult);
        }

        public Task<Result<UserProfileDto>> UpdateProfileAsync(
            uint userId,
            UpdateUserProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profileResult);
        }

        public Task<Result<UserProfileDto>> UploadAvatarAsync(
            uint userId,
            UploadAvatarRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profileResult);
        }

        public Task<Result<UserProfileDto>> UpdateLearningProfileAsync(
            uint userId,
            UpdateLearningProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profileResult);
        }

        public Task<Result<OtpSendResponse>> SendOtpAsync(
            OtpSendRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<OtpSendResponse>.Ok(new OtpSendResponse(300)));
        }

        public Task<Result<OtpVerifyResponse>> VerifyOtpAsync(
            OtpVerifyRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<OtpVerifyResponse>.Ok(new OtpVerifyResponse(true)));
        }

        public Task<Result<OtpSendResponse>> ForgotPasswordAsync(
            ForgotPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<OtpSendResponse>.Ok(new OtpSendResponse(300)));
        }

        public Task<Result<bool>> ResetPasswordAsync(
            ResetPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<bool>.Ok(true));
        }

        public Task<Result<bool>> ChangePasswordAsync(
            uint userId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<bool>.Ok(true));
        }
    }
}
