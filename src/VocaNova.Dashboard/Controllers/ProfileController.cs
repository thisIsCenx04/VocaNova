using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Net.Http.Headers;
using VocaNova.Dashboard.Models.Api.Auth;
using VocaNova.Dashboard.Models.Profile;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private const string ThemeCookie = "VocaNova.Dashboard.Theme";
    private static readonly string[] SupportedCultures = { "en", "vi" };

    private readonly IVocaNovaApiClient _api;
    private readonly IStringLocalizer<SharedResource> _l;

    public ProfileController(IVocaNovaApiClient api, IStringLocalizer<SharedResource> l)
    {
        _api = api;
        _l = l;
    }

    // ── Profile ──────────────────────────────────────────────────────────
    [HttpGet("/profile")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var me = await _api.GetAsync<AuthProfileDto>("api/auth/me", cancellationToken);
        return View(new ProfileViewModel
        {
            Profile = me.IsSuccess ? me.Data : null,
            Loaded = me.IsSuccess && me.Data is not null,
            DisplayName = me.Data?.DisplayName,
        });
    }

    [HttpPost("/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string? displayName, CancellationToken cancellationToken)
    {
        var name = displayName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 150)
        {
            SetToast(false, null, _l["Profile.Validation.Name"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.PutAsync<AuthProfileDto>("api/auth/me/profile", new { display_name = name }, cancellationToken);
        if (result.IsSuccess && result.Data is not null)
        {
            await RefreshClaimsAsync(result.Data.DisplayName, result.Data.AvatarUrl);
        }

        SetToast(result.IsSuccess, "Profile.Toast.Updated", result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/profile/avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            SetToast(false, null, _l["Profile.Validation.Avatar"].Value);
            return RedirectToAction(nameof(Index));
        }

        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        }
        content.Add(fileContent, "File", file.FileName);

        var result = await _api.PostFormAsync<AuthProfileDto>("api/auth/me/avatar", content, cancellationToken);
        if (result.IsSuccess && result.Data is not null)
        {
            await RefreshClaimsAsync(result.Data.DisplayName, result.Data.AvatarUrl);
        }

        SetToast(result.IsSuccess, "Profile.Toast.AvatarUpdated", result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/profile/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string? currentPassword, string? newPassword, string? confirmPassword, CancellationToken cancellationToken)
    {
        var error = ValidatePassword(currentPassword, newPassword, confirmPassword);
        if (error is not null)
        {
            SetToast(false, null, error);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.PutAsync<object>("api/auth/me/password", new { current_password = currentPassword, new_password = newPassword }, cancellationToken);
        SetToast(result.IsSuccess, "Profile.Toast.PasswordChanged", result.Message);
        return RedirectToAction(nameof(Index));
    }

    // ── Settings ─────────────────────────────────────────────────────────
    [HttpGet("/settings")]
    public IActionResult Settings()
    {
        return View(new SettingsViewModel
        {
            Theme = Request.Cookies[ThemeCookie] == "dark" ? "dark" : "light",
            Culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi" ? "vi" : "en",
        });
    }

    [HttpPost("/settings/theme")]
    [ValidateAntiForgeryToken]
    public IActionResult SetTheme(string? theme)
    {
        var value = theme == "dark" ? "dark" : "light";
        Response.Cookies.Append(ThemeCookie, value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            Path = "/",
        });
        SetToast(true, "Settings.Toast.Saved", null);
        return RedirectToAction(nameof(Settings));
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    // Mirror backend: current bắt buộc, new StrongPassword (≥8, hoa/thường/số), confirm khớp.
    private string? ValidatePassword(string? current, string? next, string? confirm)
    {
        if (string.IsNullOrEmpty(current))
        {
            return _l["Profile.Validation.CurrentPasswordRequired"].Value;
        }
        if (string.IsNullOrEmpty(next) || next.Length < 8 || !next.Any(char.IsUpper) || !next.Any(char.IsLower) || !next.Any(char.IsDigit))
        {
            return _l["Profile.Validation.NewPassword"].Value;
        }
        if (!string.Equals(next, confirm, StringComparison.Ordinal))
        {
            return _l["Profile.Validation.Confirm"].Value;
        }

        return null;
    }

    private async Task RefreshClaimsAsync(string displayName, string? avatarUrl)
    {
        // Giữ nguyên các claim khác, chỉ thay tên + avatar để topbar cập nhật ngay.
        var claims = User.Claims
            .Where(c => c.Type != ClaimTypes.Name && c.Type != "avatar_url")
            .ToList();
        claims.Add(new Claim(ClaimTypes.Name, displayName));
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            claims.Add(new Claim("avatar_url", avatarUrl));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var access = await HttpContext.GetTokenAsync("access_token") ?? string.Empty;
        var refresh = await HttpContext.GetTokenAsync("refresh_token") ?? string.Empty;
        var properties = new AuthenticationProperties { IsPersistent = true };
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = access },
            new AuthenticationToken { Name = "refresh_token", Value = refresh },
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    private void SetToast(bool success, string? successKey, string? errorMessage)
    {
        TempData["ToastKind"] = success ? "ok" : "err";
        TempData["ToastMessage"] = success
            ? (successKey is null ? string.Empty : _l[successKey].Value)
            : (string.IsNullOrWhiteSpace(errorMessage) ? _l["Toast.ActionFailed"].Value : errorMessage);
    }
}
