using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Dashboard.Controllers;

// Trang Settings: đổi theme (Light mặc định / Dark) + ngôn ngữ console (Tiếng Việt / English).
// Lưu lựa chọn vào cookie để _Layout render server-side.
public sealed class SettingsController : Controller
{
    private const string ThemeCookie = "VocaNova.Dashboard.Theme";
    private const string LanguageCookie = "VocaNova.Dashboard.Language";

    [HttpGet("/settings")]
    public IActionResult Index()
    {
        ViewData["Theme"] = Request.Cookies[ThemeCookie] == "dark" ? "dark" : "light";
        ViewData["Language"] = Request.Cookies[LanguageCookie] == "en" ? "en" : "vi";
        return View();
    }

    // Lưu cả Appearance + Language trong một lần (nút "Lưu thay đổi / Save Changes").
    [HttpPost("/settings")]
    [ValidateAntiForgeryToken]
    public IActionResult Save(string theme, string language)
    {
        var themeValue = theme == "dark" ? "dark" : "light";
        var languageValue = language == "en" ? "en" : "vi";

        AppendCookie(ThemeCookie, themeValue);
        AppendCookie(LanguageCookie, languageValue);

        TempData["SettingsSaved"] = "Changes saved.";
        return RedirectToAction(nameof(Index));
    }

    // Giữ endpoint cũ để tương thích (đổi nhanh theme nếu được gọi trực tiếp).
    [HttpPost("/settings/theme")]
    [ValidateAntiForgeryToken]
    public IActionResult SetTheme(string theme)
    {
        var value = theme == "dark" ? "dark" : "light";
        AppendCookie(ThemeCookie, value);
        TempData["SettingsSaved"] = value == "dark" ? "Theme set to Dark." : "Theme set to Light.";
        return RedirectToAction(nameof(Index));
    }

    private void AppendCookie(string name, string value) =>
        Response.Cookies.Append(name, value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
}
