namespace VocaNova.Dashboard.Services.Localization;

public sealed class Translator : ITranslator
{
    public const string LanguageCookie = "VocaNova.Dashboard.Language";

    private readonly IReadOnlyDictionary<string, string> _map;

    public Translator(IHttpContextAccessor accessor)
    {
        var lang = accessor.HttpContext?.Request.Cookies[LanguageCookie];
        Language = lang == "en" ? "en" : "vi"; // Mặc định Tiếng Việt (khớp Figma + Settings).
        _map = TranslationTable.Vietnamese;
    }

    public string Language { get; }

    public string this[string text]
    {
        get
        {
            if (string.IsNullOrEmpty(text) || Language != "vi")
            {
                return text;
            }

            return _map.TryGetValue(text, out var vi) ? vi : text;
        }
    }

    public string Format(string text, params object[] args) => string.Format(this[text], args);
}
