using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class QuizQuestionBuilder : IQuizQuestionBuilder
{
    private const int WordToMeaning = 1;
    private const int MeaningToWord = 2;
    private const int Description = 3;
    private const string NotEnoughDistractorsMessage = "Không đủ đáp án gây nhiễu để tạo câu hỏi";

    private readonly IQuizQuestionRepository _quizQuestionRepository;

    public QuizQuestionBuilder(IQuizQuestionRepository quizQuestionRepository)
    {
        _quizQuestionRepository = quizQuestionRepository;
    }

    public async Task<Result<QuestionDto>> BuildQuestionAsync(
        uint wordId,
        int questionType,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0)
        {
            return Result<QuestionDto>.NotFound("Word not found.");
        }

        if (questionType is not WordToMeaning and not MeaningToWord and not Description)
        {
            return Result<QuestionDto>.Fail("Question type is invalid.");
        }

        var word = await _quizQuestionRepository.FindQuestionWordAsync(wordId, cancellationToken);
        if (word is null)
        {
            return Result<QuestionDto>.NotFound("Word not found.");
        }

        var content = BuildQuestionContent(word, questionType);
        if (content is null)
        {
            return Result<QuestionDto>.Fail("Word does not have enough quiz content.");
        }

        var candidates = await _quizQuestionRepository.GetDistractorCandidatesAsync(
            word.WordId,
            word.WordClass,
            word.TopicIds,
            cancellationToken);

        var distractors = BuildDistractors(candidates, questionType, content.Value.ExpectedAnswer);
        if (distractors.Count < 3)
        {
            return Result<QuestionDto>.Fail(NotEnoughDistractorsMessage);
        }

        var choices = Shuffle(distractors
            .Take(3)
            .Append(content.Value.ExpectedAnswer)
            .ToList());

        return Result<QuestionDto>.Ok(new QuestionDto(
            word.WordId,
            word.SenseId,
            questionType,
            content.Value.DisplayContent,
            content.Value.ExpectedAnswer,
            choices));
    }

    private static (string DisplayContent, string ExpectedAnswer)? BuildQuestionContent(
        QuizQuestionWordDto word,
        int questionType)
    {
        return questionType switch
        {
            WordToMeaning when HasText(word.VietnameseMeaning) => (word.Word, word.VietnameseMeaning!.Trim()),
            MeaningToWord when HasText(word.VietnameseMeaning) => (word.VietnameseMeaning!.Trim(), word.Word),
            Description when HasText(word.EnglishDefinition) => (word.EnglishDefinition.Trim(), word.Word),
            _ => null,
        };
    }

    private static List<string> BuildDistractors(
        IReadOnlyCollection<QuizQuestionWordDto> candidates,
        int questionType,
        string expectedAnswer)
    {
        return candidates
            .Select(candidate => GetCandidateAnswer(candidate, questionType))
            .Where(HasText)
            .Select(answer => answer!.Trim())
            .Where(answer => !string.Equals(answer, expectedAnswer, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .ToList();
    }

    private static string? GetCandidateAnswer(QuizQuestionWordDto candidate, int questionType)
    {
        return questionType == WordToMeaning
            ? candidate.VietnameseMeaning
            : candidate.Word;
    }

    private static bool HasText(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private static List<string> Shuffle(List<string> choices)
    {
        for (var index = choices.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (choices[index], choices[swapIndex]) = (choices[swapIndex], choices[index]);
        }

        return choices;
    }
}
