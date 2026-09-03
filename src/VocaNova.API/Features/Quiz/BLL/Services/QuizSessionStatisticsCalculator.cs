using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public static class QuizSessionStatisticsCalculator
{
    public static void ApplyStats(QuizSubmissionState session)
    {
        var graded = GetGradedAnswers(session.Answers);
        session.CorrectCount = graded.Count(answer => answer.IsCorrect == true);
        session.WrongCount = graded.Count(answer => answer.IsCorrect == false);
        session.Score = CalculateScore(session.CorrectCount, session.QuestionCount);
        session.MaxStreak = CalculateMaxStreak(graded);
    }

    public static void ApplyStats(QuizResultState session)
    {
        var graded = GetGradedAnswers(session.Answers);
        session.CorrectCount = graded.Count(answer => answer.IsCorrect == true);
        session.WrongCount = graded.Count(answer => answer.IsCorrect == false);
        session.Score = CalculateScore(session.CorrectCount, session.QuestionCount);
        session.MaxStreak = CalculateMaxStreak(graded);
    }

    public static QuizResult ToResult(QuizResultState session)
    {
        var graded = GetGradedAnswers(session.Answers);
        var correct = graded.Count(answer => answer.IsCorrect == true);
        var wrong = graded.Count(answer => answer.IsCorrect == false);
        return new QuizResult(
            session.SessionId,
            session.Status,
            correct,
            wrong,
            session.QuestionCount,
            graded.Count,
            CalculateAccuracy(correct, graded.Count),
            CalculateDurationSec(session.StartedAt, session.EndedAt),
            CalculateMaxStreak(graded),
            CalculateScore(correct, session.QuestionCount),
            session.StartedAt,
            session.EndedAt,
            session.Answers.OrderBy(answer => answer.QuestionNumber).ThenBy(answer => answer.AnswerId).ToArray());
    }

    private static List<T> GetGradedAnswers<T>(IEnumerable<T> answers) where T : class =>
        answers.Where(answer => answer switch
            {
                QuizSubmissionAnswer submission => submission.IsCorrect.HasValue,
                TestAnswerResult result => result.IsCorrect.HasValue,
                _ => false,
            })
            .OrderBy(answer => answer switch
            {
                QuizSubmissionAnswer submission => submission.QuestionNumber,
                TestAnswerResult result => result.QuestionNumber,
                _ => 0,
            })
            .ThenBy(answer => answer switch
            {
                QuizSubmissionAnswer submission => submission.AnswerId,
                TestAnswerResult result => result.AnswerId,
                _ => 0u,
            })
            .ToList();

    private static bool? IsCorrect<T>(T answer) where T : class => answer switch
    {
        QuizSubmissionAnswer submission => submission.IsCorrect,
        TestAnswerResult result => result.IsCorrect,
        _ => null,
    };

    private static float CalculateAccuracy(int correctCount, int answeredCount) =>
        answeredCount == 0 ? 0 : (float)correctCount / answeredCount * 100;

    private static float CalculateScore(int correctCount, int questionCount) =>
        questionCount == 0 ? 0 : (float)correctCount / questionCount * 100;

    private static int CalculateMaxStreak<T>(IReadOnlyCollection<T> gradedAnswers) where T : class
    {
        var current = 0;
        var maximum = 0;
        foreach (var answer in gradedAnswers)
        {
            if (IsCorrect(answer) == true)
            {
                current++;
                maximum = Math.Max(maximum, current);
            }
            else
            {
                current = 0;
            }
        }

        return maximum;
    }

    private static int? CalculateDurationSec(DateTime startedAt, DateTime? endedAt) =>
        endedAt.HasValue ? Math.Max(0, (int)Math.Round((endedAt.Value - startedAt).TotalSeconds)) : null;
}
