using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public sealed class QuizHistoryService : IQuizHistoryService
{
    private readonly IQuizHistoryRepository _repository;

    public QuizHistoryService(IQuizHistoryRepository repository) => _repository = repository;

    public async Task<QuizOperationResult<PagedCollection<QuizHistoryItem>>> GetHistoryAsync(
        uint userId, QuizHistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (userId == 0) return QuizOperationResult<PagedCollection<QuizHistoryItem>>.Unauthorized("Unauthorized.");
        var error = ValidatePagination(query.Page, query.Limit);
        if (error is not null) return QuizOperationResult<PagedCollection<QuizHistoryItem>>.ValidationFailure(error);
        return QuizOperationResult<PagedCollection<QuizHistoryItem>>.Success(
            await _repository.GetHistoryAsync(userId, query.Page, query.Limit, cancellationToken));
    }

    public async Task<QuizOperationResult<PagedCollection<WrongWord>>> GetWrongWordsAsync(
        uint userId, WrongWordsQuery query, CancellationToken cancellationToken = default)
    {
        if (userId == 0) return QuizOperationResult<PagedCollection<WrongWord>>.Unauthorized("Unauthorized.");
        var error = ValidatePagination(query.Page, query.Limit);
        if (error is not null) return QuizOperationResult<PagedCollection<WrongWord>>.ValidationFailure(error);
        return QuizOperationResult<PagedCollection<WrongWord>>.Success(
            await _repository.GetWrongWordsAsync(userId, query.Page, query.Limit, cancellationToken));
    }

    public async Task<QuizOperationResult<bool>> ClearWrongWordAsync(
        uint userId, uint wordId, CancellationToken cancellationToken = default)
    {
        if (userId == 0) return QuizOperationResult<bool>.Unauthorized("Unauthorized.");
        if (wordId == 0) return QuizOperationResult<bool>.NotFound("Wrong word not found.");
        return await _repository.ClearWrongWordAsync(userId, wordId, cancellationToken)
            ? QuizOperationResult<bool>.Success(true)
            : QuizOperationResult<bool>.NotFound("Wrong word not found.");
    }

    private static string? ValidatePagination(int page, int limit)
    {
        if (page <= 0) return "Page must be greater than zero.";
        return limit <= 0 || limit > AppSettings.MaxPageLimit
            ? $"Limit must be between 1 and {AppSettings.MaxPageLimit}."
            : null;
    }
}
