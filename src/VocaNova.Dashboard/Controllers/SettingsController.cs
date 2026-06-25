using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Dashboard.Controllers;

// Trang Settings: đổi theme dashboard (Light mặc định / Dark). Lưu lựa chọn vào cookie để _Layout render server-side.
public sealed class SettingsController : Controller
{
    private const string ThemeCookie = "VocaNova.Dashboard.Theme";

    [HttpGet("/settings")]
    public IActionResult Index()
    {
        ViewData["Theme"] = Request.Cookies[ThemeCookie] == "dark" ? "dark" : "light";
        return View();
    }

    [HttpPost("/settings/theme")]
    [ValidateAntiForgeryToken]
    public IActionResult SetTheme(string theme)
    {
        var value = theme == "dark" ? "dark" : "light";
        Response.Cookies.Append(ThemeCookie, value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });

        TempData["SettingsSaved"] = value == "dark" ? "Theme set to Dark." : "Theme set to Light.";
        return RedirectToAction(nameof(Index));
    }
}
