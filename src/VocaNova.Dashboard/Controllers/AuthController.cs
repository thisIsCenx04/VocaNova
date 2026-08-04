using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Auth;
using VocaNova.Dashboard.Services.Auth;

namespace VocaNova.Dashboard.Controllers;

public sealed class AuthController : Controller
{
    private const string AccessTokenProperty = "access_token";
    private const string RefreshTokenProperty = "refresh_token";

    private readonly IDashboardAuthService _authService;

    public AuthController(IDashboardAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDashboard();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(
            model.Phone!.Trim(),
            model.Password!,
            cancellationToken);

        if (!result.IsSuccess || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to sign in.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User.UserId.ToString(CultureInfo.InvariantCulture)),
            new("user_id", result.User.UserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, result.User.DisplayName),
            new(ClaimTypes.Role, result.User.Role),
            new("role", result.User.Role),
            new("status", result.User.Status),
        };

        if (!string.IsNullOrWhiteSpace(result.User.Phone))
        {
            claims.Add(new Claim("phone", result.User.Phone));
        }

        if (!string.IsNullOrWhiteSpace(result.User.AvatarUrl))
        {
            claims.Add(new Claim("avatar_url", result.User.AvatarUrl));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            // Cookie sống 30 phút (sliding); access token backend TTL ngắn hơn sẽ được refresh ở các feature data sau.
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
        };
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = AccessTokenProperty, Value = result.AccessToken! },
            new AuthenticationToken { Name = RefreshTokenProperty, Value = result.RefreshToken! },
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToDashboard(result.User.Role);
    }

    [Authorize]
    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessToken = await HttpContext.GetTokenAsync(AccessTokenProperty) ?? string.Empty;
        var refreshToken = await HttpContext.GetTokenAsync(RefreshTokenProperty) ?? string.Empty;

        await _authService.LogoutAsync(accessToken, refreshToken, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToDashboard(string? role = null)
    {
        role ??= User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (string.Equals(role, "super_admin", StringComparison.Ordinal))
        {
            return RedirectToAction("Index", "AdminAccounts");
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
