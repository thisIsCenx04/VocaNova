using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class QuizHistoryService : IQuizHistoryService
{
    private readonly IQuizHistoryRepository _quizHistoryRepository;

    public QuizHistoryService(IQuizHistoryRepository quizHistoryRepository)
    {
        _quizHistoryRepository = quizHistoryRepository;
    }

    public async Task<Result<PagedResult<QuizHistoryItemDto>>> GetHistoryAsync(
        uint userId,
        QuizHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<PagedResult<QuizHistoryItemDto>>.Unauthorized("Unauthorized.");
        }

        var validationResult = ValidatePagination(query.Page, query.Limit);
        if (!validationResult.IsSuccess)
        {
            return Result<PagedResult<QuizHistoryItemDto>>.Fail(validationResult.Error!);
        }

        var history = await _quizHistoryRepository.GetHistoryAsync(
            userId,
            query.Page,
            query.Limit,
            cancellationToken);

        return Result<PagedResult<QuizHistoryItemDto>>.Ok(history);
    }

    public async Task<Result<PagedResult<WrongWordDto>>> GetWrongWordsAsync(
        uint userId,
        WrongWordsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<PagedResult<WrongWordDto>>.Unauthorized("Unauthorized.");
        }

        var validationResult = ValidatePagination(query.Page, query.Limit);
        if (!validationResult.IsSuccess)
        {
            return Result<PagedResult<WrongWordDto>>.Fail(validationResult.Error!);
        }

        var wrongWords = await _quizHistoryRepository.GetWrongWordsAsync(
            userId,
            query.Page,
            query.Limit,
            cancellationToken);

        return Result<PagedResult<WrongWordDto>>.Ok(wrongWords);
    }

    public async Task<Result<bool>> ClearWrongWordAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<bool>.Unauthorized("Unauthorized.");
        }

        if (wordId == 0)
        {
            return Result<bool>.NotFound("Wrong word not found.");
        }

        var cleared = await _quizHistoryRepository.ClearWrongWordAsync(
            userId,
            wordId,
            cancellationToken);
        if (!cleared)
        {
            return Result<bool>.NotFound("Wrong word not found.");
        }

        return Result<bool>.Ok(true);
    }

    private static Result<bool> ValidatePagination(int page, int limit)
    {
        if (page <= 0)
        {
            return Result<bool>.Fail("Page must be greater than zero.");
        }

        if (limit <= 0 || limit > AppSettings.MaxPageLimit)
        {
            return Result<bool>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        return Result<bool>.Ok(true);
    }
}
