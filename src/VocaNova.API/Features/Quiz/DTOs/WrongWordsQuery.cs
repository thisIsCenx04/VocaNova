using VocaNova.API.Common.Constants;

namespace VocaNova.API.Features.Quiz.DTOs;

public sealed class WrongWordsQuery
{
    public int Page { get; set; } = AppSettings.DefaultPage;

    public int Limit { get; set; } = AppSettings.DefaultPageLimit;
}
