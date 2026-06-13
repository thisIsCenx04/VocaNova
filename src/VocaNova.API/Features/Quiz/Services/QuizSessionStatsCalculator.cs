using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Services;

public static class QuizSessionStatsCalculator
{
    public static void ApplyStats(TestSession session)
    {
        var gradedAnswers = GetGradedAnswers(session);

        session.CorrectCount = gradedAnswers.Count(answer => answer.IsCorrect == true);
        session.WrongCount = gradedAnswers.Count(answer => answer.IsCorrect == false);
        session.Score = CalculateAccuracy(session.CorrectCount, gradedAnswers.Count);
        session.MaxStreak = CalculateMaxStreak(gradedAnswers);
    }

    public static QuizResultDto ToResultDto(TestSession session)
    {
        var gradedAnswers = GetGradedAnswers(session);
        var correctCount = gradedAnswers.Count(answer => answer.IsCorrect == true);
        var wrongCount = gradedAnswers.Count(answer => answer.IsCorrect == false);
        var accuracy = CalculateAccuracy(correctCount, gradedAnswers.Count);
        var maxStreak = CalculateMaxStreak(gradedAnswers);

        return new QuizResultDto(
            session.SessionId,
            session.Status,
            correctCount,
            wrongCount,
            session.QuestionCount,
            gradedAnswers.Count,
            accuracy,
            CalculateDurationSec(session),
            maxStreak,
            accuracy,
            session.StartedAt,
            session.EndedAt,
            session.TestAnswers
                .OrderBy(answer => answer.QuestionNumber)
                .ThenBy(answer => answer.AnswerId)
                .Select(answer => new TestAnswerResultDto(
                    answer.AnswerId,
                    answer.WordId,
                    answer.SenseId,
                    answer.QuestionNumber,
                    answer.QuestionType,
                    answer.DisplayContent,
                    answer.ExpectedAnswer,
                    answer.UserAnswer,
                    answer.IsCorrect,
                    answer.AiScore,
                    answer.AiExplanation,
                    answer.AiSuggestion))
                .ToArray());
    }

    private static List<TestAnswer> GetGradedAnswers(TestSession session)
    {
        return session.TestAnswers
            .Where(answer => answer.IsCorrect.HasValue)
            .OrderBy(answer => answer.QuestionNumber)
            .ThenBy(answer => answer.AnswerId)
            .ToList();
    }

    private static float CalculateAccuracy(int correctCount, int answeredCount)
    {
        return answeredCount == 0
            ? 0
            : (float)correctCount / answeredCount * 100;
    }

    private static int CalculateMaxStreak(IReadOnlyCollection<TestAnswer> gradedAnswers)
    {
        var currentStreak = 0;
        var maxStreak = 0;
        foreach (var answer in gradedAnswers)
        {
            if (answer.IsCorrect == true)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak;
    }

    private static int? CalculateDurationSec(TestSession session)
    {
        if (!session.EndedAt.HasValue)
        {
            return null;
        }

        return Math.Max(0, (int)Math.Round((session.EndedAt.Value - session.StartedAt).TotalSeconds));
    }
}
