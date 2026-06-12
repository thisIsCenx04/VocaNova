using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Auth.Controllers;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Services;

namespace VocaNova.Tests.Auth;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_Should_Return_201_When_Service_Succeeds()
    {
        var controller = CreateController(new StubAuthService(
            Result<TokenResponse>.Ok(new TokenResponse("access-token", "refresh-token", 900))));

        var result = await controller.Register(
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A"),
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
            new RegisterRequest("0912345678", "Password1", "Nguyen Van A"),
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

    private static AuthController CreateController(IAuthService authService)
    {
        return new AuthController(authService)
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

        public StubAuthService(Result<TokenResponse> result)
        {
            _result = result;
        }

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
            return Task.FromResult(_result);
        }

        public Task<Result<TokenResponse>> GoogleLoginAsync(
            GoogleLoginRequest request,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }
}
