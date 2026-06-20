using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Dashboard.Controllers;

/// <summary>Đổi ngôn ngữ (R2): ghi culture cookie rồi quay lại trang cũ.</summary>
public sealed class CultureController : Controller
{
    private static readonly string[] Supported = { "en", "vi" };

    [HttpPost("/culture/set")]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string? returnUrl = null)
    {
        if (Supported.Contains(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/",
                });
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
