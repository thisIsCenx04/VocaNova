namespace VocaNova.Dashboard.Services.Localization;

// Dịch UI dashboard theo cookie ngôn ngữ (VocaNova.Dashboard.Language: "vi" | "en").
// Quy ước: KEY chính là chuỗi tiếng Anh. Ở chế độ English trả về key; chế độ Tiếng Việt
// tra bảng dịch, không có thì fallback về tiếng Anh (key).
public interface ITranslator
{
    string Language { get; }

    string this[string text] { get; }

    string Format(string text, params object[] args);
}
