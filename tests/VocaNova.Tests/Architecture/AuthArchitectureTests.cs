using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Abstractions.Transactions;
using VocaNova.API.Features.Auth.Controllers;

namespace VocaNova.Tests.Architecture;

public sealed class AuthArchitectureTests
{
    [Fact]
    public void Controller_Should_Preserve_Routes_Verbs_And_Authorization()
    {
        typeof(AuthController).GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/auth");

        AssertRoute(nameof(AuthController.Register), typeof(HttpPostAttribute), "register", allowAnonymous: true);
        AssertRoute(nameof(AuthController.Login), typeof(HttpPostAttribute), "login", allowAnonymous: true);
        AssertRoute(nameof(AuthController.GoogleLogin), typeof(HttpPostAttribute), "google", allowAnonymous: true);
        AssertRoute(nameof(AuthController.RefreshToken), typeof(HttpPostAttribute), "refresh", allowAnonymous: true);
        AssertRoute(nameof(AuthController.SendOtp), typeof(HttpPostAttribute), "otp/send", allowAnonymous: true);
        AssertRoute(nameof(AuthController.VerifyOtp), typeof(HttpPostAttribute), "otp/verify", allowAnonymous: true);
        AssertRoute(nameof(AuthController.ForgotPassword), typeof(HttpPostAttribute), "forgot-password", allowAnonymous: true);
        AssertRoute(nameof(AuthController.VerifyResetOtp), typeof(HttpPostAttribute), "reset-password/verify-otp", allowAnonymous: true);
        AssertRoute(nameof(AuthController.ResetPassword), typeof(HttpPostAttribute), "reset-password", allowAnonymous: true);

        AssertRoute(nameof(AuthController.Logout), typeof(HttpPostAttribute), "logout", authorize: true);
        AssertRoute(nameof(AuthController.ChangePassword), typeof(HttpPutAttribute), "me/password", authorize: true);
        AssertRoute(nameof(AuthController.GetMe), typeof(HttpGetAttribute), "me", authorize: true);
        AssertRoute(nameof(AuthController.DeleteMe), typeof(HttpDeleteAttribute), "me", authorize: true);
        AssertRoute(nameof(AuthController.UpdateProfile), typeof(HttpPutAttribute), "me/profile", authorize: true);
        AssertRoute(nameof(AuthController.UploadAvatar), typeof(HttpPostAttribute), "me/avatar", authorize: true);
        AssertRoute(nameof(AuthController.UpdateLearningProfile), typeof(HttpPutAttribute), "me/learning-profile", authorize: true);
    }

    [Fact]
    public void Controller_Service_Dal_And_Providers_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(AuthController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(
                typeof(IAuthService),
                typeof(IAuthRateLimiter),
                typeof(IOptions<AuthRateLimitOptions>));

        typeof(AuthService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().ContainInOrder(
                typeof(IAuthAccountRepository),
                typeof(IRefreshTokenRepository),
                typeof(IOtpRepository),
                typeof(IApplicationTransactionManager),
                typeof(IJwtTokenService),
                typeof(IGoogleIdentityProvider),
                typeof(IPasswordHasher),
                typeof(IRefreshTokenHasher),
                typeof(IOptions<AuthTokenOptions>));

        typeof(AuthAccountRepository).Should().Implement<IAuthAccountRepository>();
        typeof(RefreshTokenRepository).Should().Implement<IRefreshTokenRepository>();
        typeof(OtpRepository).Should().Implement<IOtpRepository>();
        typeof(EfApplicationTransactionManager).Should().Implement<IApplicationTransactionManager>();
        typeof(JwtTokenService).Should().Implement<IJwtTokenService>();
        typeof(GoogleTokenVerifier).Should().Implement<IGoogleIdentityProvider>();
        typeof(BcryptPasswordHasher).Should().Implement<IPasswordHasher>();
        typeof(Sha256RefreshTokenHasher).Should().Implement<IRefreshTokenHasher>();
        typeof(RandomOtpCodeGenerator).Should().Implement<IOtpCodeGenerator>();
        typeof(ConsoleSmsProvider).Should().Implement<ISmsSender>();
        typeof(SpeedSmsProvider).Should().Implement<ISmsSender>();
        typeof(CloudinaryAvatarStorage).Should().Implement<IAvatarStorage>();
        typeof(RedisUserProfileCache).Should().Implement<IUserProfileCache>();
        typeof(InMemoryAuthRateLimiter).Should().Implement<IAuthRateLimiter>();
    }

    [Fact]
    public void Auth_Bll_Source_Should_Not_Reference_Http_Ef_Dal_Contracts_Redis_Or_Infrastructure()
    {
        var root = FindRepositoryRoot();
        var bllRoot = Path.Combine(root, "src", "VocaNova.API", "Features", "Auth", "BLL");
        var forbidden = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "VocaNova.API.Features.Auth.Contracts",
            "VocaNova.API.Features.Auth.DAL",
            "VocaNova.API.Infrastructure",
            "StackExchange.Redis",
            "VocaNovaDbContext",
            "IFormFile",
            "StatusCodes",
        };

        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        foreach (var token in forbidden)
        {
            File.ReadAllText(file).Should().NotContain(
                token,
                $"Auth BLL source {Path.GetRelativePath(root, file)} must remain framework-neutral");
        }
    }

    private static void AssertRoute(
        string methodName,
        Type attributeType,
        string template,
        bool allowAnonymous = false,
        bool authorize = false)
    {
        var method = typeof(AuthController).GetMethod(methodName)!;
        method.GetCustomAttributes(attributeType, false)
            .Cast<HttpMethodAttribute>()
            .Single()
            .Template
            .Should()
            .Be(template);

        if (allowAnonymous)
        {
            method.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
        }

        if (authorize)
        {
            method.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VocaNova.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
