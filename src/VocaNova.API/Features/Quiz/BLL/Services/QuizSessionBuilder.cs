using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public sealed class QuizSessionBuilder : IQuizSessionBuilder
{
    private const string NotEnoughWordsMessage = "Không đủ từ để tạo bài kiểm tra";

    private readonly IQuizPoolRepository _quizPoolRepository;

    public QuizSessionBuilder(IQuizPoolRepository quizPoolRepository)
    {
        _quizPoolRepository = quizPoolRepository;
    }

    public async Task<QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>> BuildPoolAsync(
        uint userId,
        BuildQuizPoolCommand request,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.Unauthorized("Unauthorized.");
        }

        if (!ScopeType.Values.Contains(request.ScopeType))
        {
            return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.ValidationFailure("Scope type is invalid.");
        }

        if (!WordOrder.All.Contains(request.WordOrder))
        {
            return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.ValidationFailure("Word order is invalid.");
        }

        if (!AnswerMethod.All.Contains(request.AnswerMethod))
        {
            return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.ValidationFailure("Answer method is invalid.");
        }

        if (request.WordLimit <= 0)
        {
            return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.ValidationFailure("Word limit must be greater than zero.");
        }

        var dateRangeResult = BuildDateRange(request);
        if (!dateRangeResult.IsSuccess)
        {
            return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.ValidationFailure(dateRangeResult.Error!);
        }

        var (addedFrom, addedBefore) = dateRangeResult.Value;
        var candidates = await _quizPoolRepository.GetCandidatesAsync(
            userId,
            request with
            {
                ScopeDateFrom = addedFrom.HasValue ? DateOnly.FromDateTime(addedFrom.Value) : null,
                ScopeDateTo = addedBefore.HasValue ? DateOnly.FromDateTime(addedBefore.Value.AddDays(-1)) : null,
                TopicIds = NormalizeTopicIds(request.TopicIds),
            },
            cancellationToken);

        var orderedCandidates = ApplyWordOrder(candidates, request.WordOrder);

        // Multiple choice needs at least 4 available words. Check the full
        // candidate set BEFORE applying the question-count limit, so requesting
        // fewer questions than the source has (e.g. 3 of 4) does not spuriously
        // fail — word_limit caps the number of questions, not the word pool.
        if (request.AnswerMethod == AnswerMethod.MultipleChoice && orderedCandidates.Count < 4)
        {
            return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.ValidationFailure(NotEnoughWordsMessage);
        }

        if (request.WordLimit.HasValue)
        {
            orderedCandidates = orderedCandidates.Take(request.WordLimit.Value).ToList();
        }

        return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.Success(orderedCandidates);
    }

    private static QuizOperationResult<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildDateRange(
        BuildQuizPoolCommand request)
    {
        return request.ScopeType switch
        {
            ScopeType.All => QuizOperationResult<(DateTime?, DateTime?)>.Success((null, null)),
            ScopeType.WrongWords => QuizOperationResult<(DateTime?, DateTime?)>.Success((null, null)),
            ScopeType.DateRange => BuildDateRangeScope(request),
            ScopeType.StartDate => BuildStartDateScope(request),
            ScopeType.EndDate => BuildEndDateScope(request),
            _ => QuizOperationResult<(DateTime?, DateTime?)>.ValidationFailure("Scope type is invalid."),
        };
    }

    private static QuizOperationResult<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildDateRangeScope(
        BuildQuizPoolCommand request)
    {
        if (!request.ScopeDateFrom.HasValue || !request.ScopeDateTo.HasValue)
        {
            return QuizOperationResult<(DateTime?, DateTime?)>.ValidationFailure("Date range requires both start and end dates.");
        }

        if (request.ScopeDateFrom.Value > request.ScopeDateTo.Value)
        {
            return QuizOperationResult<(DateTime?, DateTime?)>.ValidationFailure("Start date must be before or equal to end date.");
        }

        return QuizOperationResult<(DateTime?, DateTime?)>.Success((
            request.ScopeDateFrom.Value.ToDateTime(TimeOnly.MinValue),
            request.ScopeDateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    }

    private static QuizOperationResult<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildStartDateScope(
        BuildQuizPoolCommand request)
    {
        if (!request.ScopeDateFrom.HasValue)
        {
            return QuizOperationResult<(DateTime?, DateTime?)>.ValidationFailure("Start date scope requires a start date.");
        }

        return QuizOperationResult<(DateTime?, DateTime?)>.Success((
            request.ScopeDateFrom.Value.ToDateTime(TimeOnly.MinValue),
            null));
    }

    private static QuizOperationResult<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildEndDateScope(
        BuildQuizPoolCommand request)
    {
        if (!request.ScopeDateTo.HasValue)
        {
            return QuizOperationResult<(DateTime?, DateTime?)>.ValidationFailure("End date scope requires an end date.");
        }

        return QuizOperationResult<(DateTime?, DateTime?)>.Success((
            null,
            request.ScopeDateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    }

    private static IReadOnlyCollection<uint>? NormalizeTopicIds(IReadOnlyCollection<uint>? topicIds)
    {
        var normalized = topicIds?
            .Where(topicId => topicId > 0)
            .Distinct()
            .ToArray();

        return normalized is { Length: > 0 } ? normalized : null;
    }

    private static List<QuizPoolWord> ApplyWordOrder(
        IReadOnlyCollection<QuizPoolWord> candidates,
        string wordOrder)
    {
        return wordOrder switch
        {
            WordOrder.Oldest => candidates
                .OrderBy(candidate => candidate.AddedAt)
                .ThenBy(candidate => candidate.WordId)
                .ToList(),
            WordOrder.Random => Shuffle(candidates.ToList()),
            WordOrder.ByDifficulty => candidates
                .OrderByDescending(candidate => candidate.WrongCount)
                .ThenBy(candidate => candidate.WordId)
                .ToList(),
            _ => candidates
                .OrderByDescending(candidate => candidate.AddedAt)
                .ThenByDescending(candidate => candidate.WordId)
                .ToList(),
        };
    }

    private static List<QuizPoolWord> Shuffle(List<QuizPoolWord> candidates)
    {
        for (var index = candidates.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
        }

        return candidates;
    }
}
