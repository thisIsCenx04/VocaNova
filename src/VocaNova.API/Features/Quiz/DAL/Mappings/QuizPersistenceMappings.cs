using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Infrastructure.Persistence.Entities;
using BusinessProgress = VocaNova.API.Features.Quiz.BLL.Models.UserWordProgress;
using PersistenceProgress = VocaNova.API.Infrastructure.Persistence.Entities.UserWordProgress;

namespace VocaNova.API.Features.Quiz.DAL.Mappings;

public static class QuizPersistenceMappings
{
    public static QuizSession ToBusinessSession(this TestSession session, uint? listId) => new(
        session.SessionId, session.TestType, session.Mode, session.QuestionType, session.ScopeType,
        session.ScopeDateFrom, session.ScopeDateTo, session.WordOrder, session.WordLimit,
        session.TimeLimitSec, session.Lives, session.QuestionCount, session.Status, session.StartedAt,
        session.TestSessionTopics.Select(topic => topic.TopicId).OrderBy(id => id).ToArray(), listId);

    public static QuizSubmissionState ToSubmissionState(this TestSession session) => new()
    {
        SessionId = session.SessionId,
        UserId = session.UserId,
        AnswerMethod = session.TestType,
        QuestionType = session.QuestionType,
        ScopeType = session.ScopeType,
        ScopeDateFrom = session.ScopeDateFrom,
        ScopeDateTo = session.ScopeDateTo,
        WordOrder = session.WordOrder,
        QuestionCount = session.QuestionCount,
        Status = session.Status,
        CorrectCount = session.CorrectCount,
        WrongCount = session.WrongCount,
        Score = session.Score,
        MaxStreak = session.MaxStreak,
        EndedAt = session.EndedAt,
        TopicIds = session.TestSessionTopics.Select(topic => topic.TopicId).ToArray(),
        Answers = session.TestAnswers.Select(answer => new QuizSubmissionAnswer
        {
            AnswerId = answer.AnswerId,
            WordId = answer.WordId,
            SenseId = answer.SenseId,
            QuestionNumber = answer.QuestionNumber,
            QuestionType = answer.QuestionType,
            DisplayContent = answer.DisplayContent,
            ExpectedAnswer = answer.ExpectedAnswer,
            AcceptedAnswersJson = answer.AcceptedAnswers,
            UserAnswer = answer.UserAnswer,
            IsCorrect = answer.IsCorrect,
            AiScore = answer.AiScore,
            AiExplanation = answer.AiExplanation,
            AiSuggestion = answer.AiSuggestion,
        }).ToList(),
    };

    public static TestAnswerResult ToBusinessAnswer(this TestAnswer answer) => new(
        answer.AnswerId, answer.WordId, answer.SenseId, answer.QuestionNumber,
        answer.QuestionType, answer.DisplayContent, answer.ExpectedAnswer, answer.UserAnswer,
        answer.IsCorrect, answer.AiScore, answer.AiExplanation, answer.AiSuggestion);

    public static BusinessProgress ToBusinessProgress(this PersistenceProgress progress) => new()
    {
        ProgressId = progress.ProgressId,
        UserId = progress.UserId,
        WordId = progress.WordId,
        TestCount = progress.TestCount,
        CorrectCount = progress.CorrectCount,
        WrongCount = progress.WrongCount,
        ConsecutiveCorrect = progress.ConsecutiveCorrect,
        IsInWrongList = progress.IsInWrongList,
        MasteryLevel = progress.MasteryLevel,
        SrsInterval = progress.SrsInterval,
        EaseFactor = progress.EaseFactor,
        LastTestedAt = progress.LastTestedAt,
        LastWrongAt = progress.LastWrongAt,
        NextReviewAt = progress.NextReviewAt,
        UpdatedAt = progress.UpdatedAt,
    };

    public static void Apply(this BusinessProgress source, PersistenceProgress target)
    {
        target.TestCount = source.TestCount;
        target.CorrectCount = source.CorrectCount;
        target.WrongCount = source.WrongCount;
        target.ConsecutiveCorrect = source.ConsecutiveCorrect;
        target.IsInWrongList = source.IsInWrongList;
        target.MasteryLevel = source.MasteryLevel;
        target.SrsInterval = source.SrsInterval;
        target.EaseFactor = source.EaseFactor;
        target.LastTestedAt = source.LastTestedAt;
        target.LastWrongAt = source.LastWrongAt;
        target.NextReviewAt = source.NextReviewAt;
        target.UpdatedAt = source.UpdatedAt;
    }
}
