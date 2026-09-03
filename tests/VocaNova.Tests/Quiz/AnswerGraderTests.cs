using FluentAssertions;
using VocaNova.API.Features.Quiz.BLL.Services;

namespace VocaNova.Tests.Quiz;

public class AnswerGraderTests
{
    [Fact]
    public async Task ExactTypingGrader_Should_Ignore_Case_And_Trailing_Punctuation()
    {
        var grader = new ExactTypingGrader();

        var result = await grader.GradeAsync("  Hello!!! ", "hello");

        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task ExactTypingGrader_Should_Match_Accepted_Answers_After_Normalizing()
    {
        var grader = new ExactTypingGrader();
        var acceptedAnswers = AcceptedAnswersParser.Parse("[\"go quickly\", \"move fast!\"]");

        var result = await grader.GradeAsync("MOVE FAST", "run", acceptedAnswers);

        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleChoiceGrader_Should_Compare_Directly_Without_Normalizing()
    {
        var grader = new MultipleChoiceGrader();

        var result = await grader.GradeAsync("hello", "Hello");

        result.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleChoiceGrader_Should_Match_Accepted_Answers_Directly()
    {
        var grader = new MultipleChoiceGrader();
        var acceptedAnswers = AcceptedAnswersParser.Parse("[\"A\", \"B\"]");

        var result = await grader.GradeAsync("B", "C", acceptedAnswers);

        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void AcceptedAnswersJsonParser_Should_Return_Empty_When_Json_Is_Invalid()
    {
        var result = AcceptedAnswersParser.Parse("not-json");

        result.Should().BeEmpty();
    }
}
