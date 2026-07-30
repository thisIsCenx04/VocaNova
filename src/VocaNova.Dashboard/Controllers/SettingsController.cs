using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Api.Settings;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// Trang Settings: đổi theme (Light mặc định / Dark) + ngôn ngữ console (Tiếng Việt / English)
// — lưu vào cookie để _Layout render server-side — và cấu hình AI dùng cho chấm bài
// (provider/endpoint/model/API key), lưu qua API nên có hiệu lực cho toàn hệ thống.
public sealed class SettingsController : Controller
{
    private const string ThemeCookie = "VocaNova.Dashboard.Theme";
    private const string LanguageCookie = "VocaNova.Dashboard.Language";

    private readonly IVocaNovaApiClient _apiClient;

    public SettingsController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/settings")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Theme"] = Request.Cookies[ThemeCookie] == "dark" ? "dark" : "light";
        ViewData["Language"] = Request.Cookies[LanguageCookie] == "en" ? "en" : "vi";
        ViewData["AiGrading"] = await _apiClient.GetAiGradingConfigAsync(cancellationToken);
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

    [HttpPost("/settings/ai-grading")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAiGrading(IFormCollection form, CancellationToken cancellationToken)
    {
        var input = new AiGradingConfigInput(
            Str(form, "provider"),
            Str(form, "endpoint"),
            Str(form, "model"),
            // Comma-separated in the form; an empty box means "no fallback models", which is
            // different from leaving the field untouched, so it maps to an empty list.
            SplitModels(form["fallback_models"].ToString()),
            // Left blank on purpose when the admin is not rotating the key — the API keeps
            // whichever key is currently in force.
            Str(form, "api_key"),
            IntN(form, "max_attempts"),
            IntN(form, "retry_base_delay_ms"),
            IntN(form, "attempt_timeout_seconds"),
            DblN(form, "pass_threshold"));

        var result = await _apiClient.UpdateAiGradingConfigAsync(input, cancellationToken);
        SetAiFeedback(result, "AI grading settings saved.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/settings/ai-grading/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAiGrading(CancellationToken cancellationToken)
    {
        var result = await _apiClient.ResetAiGradingConfigAsync(cancellationToken);
        SetAiFeedback(result, "AI grading settings reset to the built-in defaults.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/settings/ai-grading/test")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestAiGrading(CancellationToken cancellationToken)
    {
        var test = await _apiClient.TestAiGradingConnectionAsync(cancellationToken);
        if (test is null)
        {
            TempData["AiGradingError"] = "Unable to reach the API to run the test.";
        }
        else if (test.Succeeded)
        {
            TempData["AiGradingSaved"] =
                $"Connection OK — {test.Model} responded in {test.ElapsedMs} ms.";
        }
        else
        {
            TempData["AiGradingError"] = $"Connection failed — {test.Message}";
        }

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

    private void SetAiFeedback(ApiActionResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            TempData["AiGradingSaved"] = successMessage;
            return;
        }

        TempData["AiGradingError"] = result.StatusCode switch
        {
            400 => result.Message ?? "Validation failed.",
            401 or 403 => "You do not have permission to change AI grading settings.",
            _ => result.Message ?? "The request could not be completed.",
        };
    }

    private static string? Str(IFormCollection form, string key)
    {
        var value = form[key].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? IntN(IFormCollection form, string key) =>
        int.TryParse(form[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static double? DblN(IFormCollection form, string key) =>
        double.TryParse(form[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static string[] SplitModels(string value) =>
        value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

    private void AppendCookie(string name, string value) =>
        Response.Cookies.Append(name, value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
}
