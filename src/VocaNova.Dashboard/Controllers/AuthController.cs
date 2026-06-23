using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VocaNova.Dashboard.Models.Auth;
using VocaNova.Dashboard.Services.Auth;

namespace VocaNova.Dashboard.Controllers;

public sealed class AuthController : Controller
{
    private const string AccessTokenProperty = "access_token";
    private const string RefreshTokenProperty = "refresh_token";

    private readonly IDashboardAuthService _authService;
    private readonly IStringLocalizer<SharedResource> _l;

    public AuthController(IDashboardAuthService authService, IStringLocalizer<SharedResource> l)
    {
        _authService = authService;
        _l = l;
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
            ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(result.ExpiresIn, 300)),
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

        return RedirectToDashboard();
    }

    [HttpGet("/forgot-password")]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDashboard();
        }

        return View(new ForgotPasswordViewModel());
    }

    // Bước 1: nhập số điện thoại → gửi OTP đặt lại mật khẩu.
    [HttpPost("/forgot-password")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReset(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        model.Step = "request";
        if (string.IsNullOrWhiteSpace(model.Phone) || !System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^0[3-9]\d{8}$"))
        {
            ModelState.AddModelError(nameof(model.Phone), _l["Forgot.Validation.Phone"].Value);
            return View(nameof(ForgotPassword), model);
        }

        var result = await _authService.ForgotPasswordAsync(model.Phone.Trim(), cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, MapError(result));
            return View(nameof(ForgotPassword), model);
        }

        // Sang bước 2: nhập OTP + mật khẩu mới.
        model.Step = "reset";
        model.ExpiresIn = result.ExpiresIn;
        model.Info = _l["Forgot.OtpSent"].Value;
        return View(nameof(ForgotPassword), model);
    }

    // Bước 2: nhập OTP + mật khẩu mới → đặt lại mật khẩu.
    [HttpPost("/forgot-password/reset")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReset(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        model.Step = "reset";

        var error = ValidateReset(model);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return View(nameof(ForgotPassword), model);
        }

        var result = await _authService.ResetPasswordAsync(
            model.Phone!.Trim(),
            model.OtpCode!.Trim(),
            model.NewPassword!,
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, MapError(result));
            return View(nameof(ForgotPassword), model);
        }

        TempData["AuthInfo"] = _l["Forgot.ResetSuccess"].Value;
        return RedirectToAction(nameof(Login));
    }

    // Mirror backend StrongPassword (≥8, có hoa/thường/số) + OTP 6 số.
    private string? ValidateReset(ForgotPasswordViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Phone) || !System.Text.RegularExpressions.Regex.IsMatch(m.Phone, @"^0[3-9]\d{8}$"))
        {
            return _l["Forgot.Validation.Phone"].Value;
        }
        if (string.IsNullOrWhiteSpace(m.OtpCode) || !System.Text.RegularExpressions.Regex.IsMatch(m.OtpCode, @"^\d{6}$"))
        {
            return _l["Forgot.Validation.Otp"].Value;
        }
        var pwd = m.NewPassword;
        if (string.IsNullOrEmpty(pwd) || pwd.Length < 8 || !pwd.Any(char.IsUpper) || !pwd.Any(char.IsLower) || !pwd.Any(char.IsDigit))
        {
            return _l["Forgot.Validation.Password"].Value;
        }
        if (!string.Equals(pwd, m.ConfirmPassword, StringComparison.Ordinal))
        {
            return _l["Forgot.Validation.Confirm"].Value;
        }

        return null;
    }

    // 429 = rate limit, 401 = OTP sai/hết hạn, 409 = OTP đã dùng, 404 = không có tài khoản.
    private string MapError(DashboardActionResult result)
    {
        return result.StatusCode switch
        {
            429 => _l["Forgot.Error.RateLimit"].Value,
            401 => _l["Forgot.Error.InvalidOtp"].Value,
            409 => _l["Forgot.Error.OtpUsed"].Value,
            404 => _l["Forgot.Error.NotFound"].Value,
            _ => string.IsNullOrWhiteSpace(result.Message) ? _l["Forgot.Error.Generic"].Value : result.Message!,
        };
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

    private IActionResult RedirectToDashboard()
    {
        return RedirectToAction("Index", "Dashboard");
    }
}
