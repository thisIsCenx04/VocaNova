using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class QuizSessionBuilder : IQuizSessionBuilder
{
    private const string NotEnoughWordsMessage = "Không đủ từ để tạo bài kiểm tra";

    private readonly IQuizWordPoolRepository _quizWordPoolRepository;

    public QuizSessionBuilder(IQuizWordPoolRepository quizWordPoolRepository)
    {
        _quizWordPoolRepository = quizWordPoolRepository;
    }

    public async Task<Result<IReadOnlyCollection<QuizPoolWordDto>>> BuildPoolAsync(
        uint userId,
        BuildQuizPoolRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<IReadOnlyCollection<QuizPoolWordDto>>.Unauthorized("Unauthorized.");
        }

        if (!ScopeType.Values.Contains(request.ScopeType))
        {
            return Result<IReadOnlyCollection<QuizPoolWordDto>>.Fail("Scope type is invalid.");
        }

        if (!WordOrder.All.Contains(request.WordOrder))
        {
            return Result<IReadOnlyCollection<QuizPoolWordDto>>.Fail("Word order is invalid.");
        }

        if (!AnswerMethod.All.Contains(request.AnswerMethod))
        {
            return Result<IReadOnlyCollection<QuizPoolWordDto>>.Fail("Answer method is invalid.");
        }

        if (request.WordLimit <= 0)
        {
            return Result<IReadOnlyCollection<QuizPoolWordDto>>.Fail("Word limit must be greater than zero.");
        }

        var dateRangeResult = BuildDateRange(request);
        if (!dateRangeResult.IsSuccess)
        {
            return Result<IReadOnlyCollection<QuizPoolWordDto>>.Fail(dateRangeResult.Error!);
        }

        var (addedFrom, addedBefore) = dateRangeResult.Value;
        var candidates = await _quizWordPoolRepository.GetCandidatesAsync(
            userId,
            addedFrom,
            addedBefore,
            NormalizeTopicIds(request.TopicIds),
            request.ListId,
            request.ScopeType == ScopeType.WrongWords,
            cancellationToken);

        var orderedCandidates = ApplyWordOrder(candidates, request.WordOrder);
        if (request.WordLimit.HasValue)
        {
            orderedCandidates = orderedCandidates.Take(request.WordLimit.Value).ToList();
        }

        if (request.AnswerMethod == AnswerMethod.MultipleChoice && orderedCandidates.Count < 4)
        {
            return Result<IReadOnlyCollection<QuizPoolWordDto>>.Fail(NotEnoughWordsMessage);
        }

        return Result<IReadOnlyCollection<QuizPoolWordDto>>.Ok(orderedCandidates);
    }

    private static Result<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildDateRange(
        BuildQuizPoolRequest request)
    {
        return request.ScopeType switch
        {
            ScopeType.All => Result<(DateTime?, DateTime?)>.Ok((null, null)),
            ScopeType.WrongWords => Result<(DateTime?, DateTime?)>.Ok((null, null)),
            ScopeType.DateRange => BuildDateRangeScope(request),
            ScopeType.StartDate => BuildStartDateScope(request),
            ScopeType.EndDate => BuildEndDateScope(request),
            _ => Result<(DateTime?, DateTime?)>.Fail("Scope type is invalid."),
        };
    }

    private static Result<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildDateRangeScope(
        BuildQuizPoolRequest request)
    {
        if (!request.ScopeDateFrom.HasValue || !request.ScopeDateTo.HasValue)
        {
            return Result<(DateTime?, DateTime?)>.Fail("Date range requires both start and end dates.");
        }

        if (request.ScopeDateFrom.Value > request.ScopeDateTo.Value)
        {
            return Result<(DateTime?, DateTime?)>.Fail("Start date must be before or equal to end date.");
        }

        return Result<(DateTime?, DateTime?)>.Ok((
            request.ScopeDateFrom.Value.ToDateTime(TimeOnly.MinValue),
            request.ScopeDateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    }

    private static Result<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildStartDateScope(
        BuildQuizPoolRequest request)
    {
        if (!request.ScopeDateFrom.HasValue)
        {
            return Result<(DateTime?, DateTime?)>.Fail("Start date scope requires a start date.");
        }

        return Result<(DateTime?, DateTime?)>.Ok((
            request.ScopeDateFrom.Value.ToDateTime(TimeOnly.MinValue),
            null));
    }

    private static Result<(DateTime? AddedFrom, DateTime? AddedBefore)> BuildEndDateScope(
        BuildQuizPoolRequest request)
    {
        if (!request.ScopeDateTo.HasValue)
        {
            return Result<(DateTime?, DateTime?)>.Fail("End date scope requires an end date.");
        }

        return Result<(DateTime?, DateTime?)>.Ok((
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

    private static List<QuizPoolWordDto> ApplyWordOrder(
        IReadOnlyCollection<QuizPoolWordDto> candidates,
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

    private static List<QuizPoolWordDto> Shuffle(List<QuizPoolWordDto> candidates)
    {
        for (var index = candidates.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
        }

        return candidates;
    }
}
