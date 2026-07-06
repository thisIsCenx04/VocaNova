using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Api.Auth;
using VocaNova.Dashboard.Models.Profile;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F063A — Admin Profile: xem/sửa hồ sơ của chính admin đang đăng nhập + đổi mật khẩu.
// BFF proxy qua IVocaNovaApiClient (token giữ server-side). Sau khi đổi tên/avatar → phát hành lại cookie để topbar cập nhật.
[Authorize]
public sealed class ProfileController : Controller
{
    private const string AccessTokenProperty = "access_token";
    private const string RefreshTokenProperty = "refresh_token";

    private readonly IVocaNovaApiClient _apiClient;

    public ProfileController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/profile")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var me = await _apiClient.GetMyProfileAsync(cancellationToken);
        if (me is null)
        {
            TempData["UserError"] = "Unable to load your profile.";
        }

        return View(BuildModel(me));
    }

    [HttpPost("/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        [FromForm] string? displayName,
        [FromForm] string? avatarUrl,
        CancellationToken cancellationToken)
    {
        var name = displayName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 150)
        {
            TempData["UserError"] = "Display name must be 2–150 characters.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _apiClient.UpdateMyProfileAsync(
            name,
            string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(),
            cancellationToken);

        await ApplyResultAndRefreshAsync(result, "Profile updated.", cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/profile/avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["UserError"] = "Please choose an image file.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var result = await _apiClient.UploadMyAvatarAsync(
            new ImageUpload(stream, file.FileName, file.ContentType),
            cancellationToken);

        await ApplyResultAndRefreshAsync(result, "Avatar updated.", cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/profile/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [FromForm] string? currentPassword,
        [FromForm] string? newPassword,
        [FromForm] string? confirmPassword,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["UserError"] = "Please fill in all password fields.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            TempData["UserError"] = "New password and confirmation do not match.";
            return RedirectToAction(nameof(Index));
        }

        if (!IsStrongPassword(newPassword))
        {
            TempData["UserError"] = "Password must be at least 8 characters with upper, lower and a digit.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _apiClient.ChangeMyPasswordAsync(currentPassword, newPassword, cancellationToken);
        if (result.IsSuccess)
        {
            TempData["UserSuccess"] = "Password changed.";
        }
        else
        {
            TempData["UserError"] = result.StatusCode switch
            {
                400 or 401 => result.Message ?? "Current password is incorrect.",
                _ => result.Message ?? "Unable to change password.",
            };
        }

        return RedirectToAction(nameof(Index));
    }

    // Xử lý kết quả cho update profile/avatar: đặt thông báo + đồng bộ lại claim (tên/avatar) để topbar đổi ngay.
    private async Task ApplyResultAndRefreshAsync(ApiActionResult result, string successMessage, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            TempData["UserError"] = result.StatusCode switch
            {
                400 => result.Message ?? "Invalid data.",
                401 or 403 => "You are not allowed to perform this action.",
                _ => result.Message ?? "The request could not be completed.",
            };
            return;
        }

        TempData["UserSuccess"] = successMessage;

        var me = await _apiClient.GetMyProfileAsync(cancellationToken);
        if (me is not null)
        {
            await RefreshIdentityAsync(me);
        }
    }

    // Phát hành lại cookie auth với display_name/avatar mới, giữ nguyên token đã lưu.
    private async Task RefreshIdentityAsync(MeProfile me)
    {
        var accessToken = await HttpContext.GetTokenAsync(AccessTokenProperty) ?? string.Empty;
        var refreshToken = await HttpContext.GetTokenAsync(RefreshTokenProperty) ?? string.Empty;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, me.UserId.ToString(CultureInfo.InvariantCulture)),
            new("user_id", me.UserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, me.DisplayName),
            new(ClaimTypes.Role, me.Role),
            new("role", me.Role),
            new("status", me.Status),
        };

        if (!string.IsNullOrWhiteSpace(me.Phone))
        {
            claims.Add(new Claim("phone", me.Phone));
        }

        if (!string.IsNullOrWhiteSpace(me.AvatarUrl))
        {
            claims.Add(new Claim("avatar_url", me.AvatarUrl));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
        };

        if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            properties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = AccessTokenProperty, Value = accessToken },
                new AuthenticationToken { Name = RefreshTokenProperty, Value = refreshToken },
            });
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    private AdminProfileViewModel BuildModel(MeProfile? me) => new()
    {
        UserId = me?.UserId ?? 0,
        DisplayName = me?.DisplayName ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
        Phone = me?.Phone ?? User.FindFirstValue("phone"),
        AvatarUrl = me?.AvatarUrl ?? User.FindFirstValue("avatar_url"),
        Role = me?.Role ?? User.FindFirstValue("role") ?? string.Empty,
        Status = me?.Status ?? "active",
    };

    // Mirror StrongPasswordValidator của API: ≥8 ký tự, có hoa + thường + số.
    private static bool IsStrongPassword(string password) =>
        password.Length >= 8
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit);
}
