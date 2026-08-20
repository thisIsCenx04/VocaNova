using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace VocaNova.Tests.Architecture;

public sealed class QuizArchitectureTests
{
    [Fact]
    public void Controller_Should_Preserve_Routes_Authorization_And_Verbs()
    {
        typeof(QuizSessionsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/quiz/sessions");
        typeof(QuizSessionsController).GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull();
        AssertRoute(nameof(QuizSessionsController.GetHistory), typeof(HttpGetAttribute), "/api/quiz/history");
        AssertRoute(nameof(QuizSessionsController.GetWrongWords), typeof(HttpGetAttribute), "/api/quiz/wrong-words");
        AssertRoute(nameof(QuizSessionsController.ClearWrongWord), typeof(HttpDeleteAttribute), "/api/quiz/wrong-words/{wordId:uint}");
        AssertRoute(nameof(QuizSessionsController.Create), typeof(HttpPostAttribute), null);
        AssertRoute(nameof(QuizSessionsController.SubmitAnswer), typeof(HttpPostAttribute), "{id:uint}/answer");
        AssertRoute(nameof(QuizSessionsController.Finish), typeof(HttpPostAttribute), "{id:uint}/finish");
        AssertRoute(nameof(QuizSessionsController.GetResult), typeof(HttpGetAttribute), "{id:uint}/result");
    }

    [Fact]
    public void Controller_And_Adapters_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(QuizSessionsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IQuizSessionService), typeof(IQuizSubmissionService),
                typeof(IQuizResultService), typeof(IQuizHistoryService));
        typeof(QuizSessionRepository).Should().Implement<IQuizSessionRepository>();
        typeof(QuizPoolRepository).Should().Implement<IQuizPoolRepository>();
        typeof(QuizQuestionRepository).Should().Implement<IQuizQuestionRepository>();
        typeof(QuizSubmissionRepository).Should().Implement<IQuizSubmissionRepository>();
        typeof(QuizResultRepository).Should().Implement<IQuizResultRepository>();
        typeof(QuizHistoryRepository).Should().Implement<IQuizHistoryRepository>();
        typeof(SrsRepository).Should().Implement<ISrsRepository>();
        typeof(RedisQuizPoolCache).Should().Implement<IQuizPoolCache>();
    }

    [Fact]
    public void Bll_Source_Should_Not_Reference_Http_Ef_Dal_Or_Infrastructure()
    {
        AssertBllBoundary("Quiz");
    }

    private static void AssertRoute(string methodName, Type attributeType, string? template)
    {
        var attribute = typeof(QuizSessionsController).GetMethod(methodName)!
            .GetCustomAttributes(attributeType, false).Cast<HttpMethodAttribute>().Single();
        attribute.Template.Should().Be(template);
    }

    private static void AssertBllBoundary(string feature)
    {
        var root = FindRepositoryRoot();
        var bllRoot = Path.Combine(root, "src", "VocaNova.API", "Features", feature, "BLL");
        var forbidden = new[]
        {
            "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore",
            $"VocaNova.API.Features.{feature}.Contracts", $"VocaNova.API.Features.{feature}.DAL",
            "VocaNova.API.Infrastructure", "StackExchange.Redis", "VocaNovaDbContext",
        };
        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        foreach (var token in forbidden)
            File.ReadAllText(file).Should().NotContain(token,
                $"{feature} BLL source {Path.GetRelativePath(root, file)} must remain boundary-independent");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VocaNova.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
