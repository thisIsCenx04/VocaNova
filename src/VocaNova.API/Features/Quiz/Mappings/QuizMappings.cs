using VocaNova.API.Common.Models;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.Contracts.Requests;
using VocaNova.API.Features.Quiz.Contracts.Responses;

namespace VocaNova.API.Features.Quiz.Mappings;

public static class QuizMappings
{
    public static CreateQuizSessionCommand ToBusinessCommand(this CreateSessionRequest request) =>
        new(request.Mode, request.QuestionType, request.ScopeType, request.ScopeDateFrom,
            request.ScopeDateTo, request.TopicIds, request.WordOrder, request.WordLimit,
            request.TimeLimitSec, request.Lives, request.AnswerMethod, request.ListId);

    public static SubmitAnswerCommand ToBusinessCommand(this SubmitAnswerRequest request) =>
        new(request.WordId, request.UserAnswer, request.ListId);

    public static QuizHistoryQuery ToBusinessQuery(this QuizHistoryRequest request) => new(request.Page, request.Limit);
    public static WrongWordsQuery ToBusinessQuery(this WrongWordsRequest request) => new(request.Page, request.Limit);

    public static CreateSessionResponse ToResponse(this CreatedQuizSession value) =>
        new(value.Session.ToResponse(), value.FirstQuestion.ToResponse());

    public static QuizSessionResponse ToResponse(this QuizSession value) =>
        new(value.SessionId, value.AnswerMethod, value.Mode, value.QuestionType, value.ScopeType,
            value.ScopeDateFrom, value.ScopeDateTo, value.WordOrder, value.WordLimit,
            value.TimeLimitSec, value.Lives, value.QuestionCount, value.Status, value.StartedAt,
            value.TopicIds, value.ListId);

    public static QuestionResponse ToResponse(this QuizQuestion value) =>
        new(value.WordId, value.SenseId, value.QuestionType, value.DisplayContent,
            value.ExpectedAnswer, value.Choices);

    public static AnswerResponse ToResponse(this QuizAnswer value) =>
        new(value.SessionId, value.WordId, value.IsCorrect, value.ExpectedAnswer,
            value.CorrectCount, value.WrongCount, value.Score, value.AiScore,
            value.AiExplanation, value.AiSuggestion, value.NextQuestion?.ToResponse());

    public static QuizResultResponse ToResponse(this QuizResult value) =>
        new(value.SessionId, value.Status, value.CorrectCount, value.WrongCount,
            value.QuestionCount, value.AnsweredCount, value.Accuracy, value.DurationSec,
            value.MaxStreak, value.Score, value.StartedAt, value.EndedAt,
            value.Answers.Select(ToResponse).ToArray());

    public static PagedResult<QuizHistoryItemResponse> ToResponse(this PagedCollection<QuizHistoryItem> value) =>
        new(value.Items.Select(item => new QuizHistoryItemResponse(item.SessionId,
            item.AnswerMethod, item.Mode, item.QuestionType, item.QuestionCount,
            item.CorrectCount, item.WrongCount, item.Accuracy, item.Score, item.MaxStreak,
            item.Status, item.StartedAt, item.EndedAt)).ToArray(),
            value.Page, value.Limit, value.TotalItems);

    public static PagedResult<WrongWordResponse> ToResponse(this PagedCollection<WrongWord> value) =>
        new(value.Items.Select(item => new WrongWordResponse(item.WordId, item.Word,
            item.PrimaryMeaning, item.TestCount, item.CorrectCount, item.WrongCount,
            item.MasteryLevel, item.LastWrongAt, item.NextReviewAt)).ToArray(),
            value.Page, value.Limit, value.TotalItems);

    private static TestAnswerResponse ToResponse(TestAnswerResult value) =>
        new(value.AnswerId, value.WordId, value.SenseId, value.QuestionNumber,
            value.QuestionType, value.DisplayContent, value.ExpectedAnswer, value.UserAnswer,
            value.IsCorrect, value.AiScore, value.AiExplanation, value.AiSuggestion);
}
