using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public interface IAnswerGrader
{
    string AnswerMethod { get; }
    Task<AnswerGrade> GradeAsync(string? userAnswer, string expectedAnswer,
        IReadOnlyCollection<string>? acceptedAnswers = null,
        CancellationToken cancellationToken = default);
}
