using VocaNova.API.Common.Constants;

namespace VocaNova.API.Features.Quiz.Contracts.Requests;

public sealed class QuizHistoryRequest
{
    public int Page { get; set; } = AppSettings.DefaultPage;

    public int Limit { get; set; } = AppSettings.DefaultPageLimit;
}
