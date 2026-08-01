using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VocaNova.Dashboard.Services.Localization;

/// <summary>
/// Dịch thông báo ModelState ngay trước khi Razor render validation summary/message.
/// DataAnnotation vẫn giữ key tiếng Anh để chế độ English hiển thị đúng nguyên bản.
/// </summary>
public sealed class LocalizedModelStateFilter : IAsyncResultFilter
{
    private readonly ITranslator _translator;

    public LocalizedModelStateFilter(ITranslator translator)
    {
        _translator = translator;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult && !context.ModelState.IsValid && _translator.Language == "vi")
        {
            foreach (var entry in context.ModelState.Values)
            {
                var messages = entry.Errors
                    .Where(error => !string.IsNullOrWhiteSpace(error.ErrorMessage))
                    .Select(error => _translator[error.ErrorMessage])
                    .ToArray();

                if (messages.Length == 0) continue;
                entry.Errors.Clear();
                foreach (var message in messages) entry.Errors.Add(message);
            }
        }

        await next();
    }
}
