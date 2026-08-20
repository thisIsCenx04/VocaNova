using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Tests.Architecture;

public class ProgressArchitectureTests
{
    [Fact]
    public void Controller_Should_Preserve_Routes_Verbs_And_Authorization()
    {
        typeof(ProgressController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/progress");
        typeof(ProgressController).GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull();

        var expectedTemplates = new Dictionary<string, string>
        {
            [nameof(ProgressController.GetSummary)] = "summary",
            [nameof(ProgressController.GetChart)] = "chart",
            [nameof(ProgressController.GetMasteryBreakdown)] = "mastery-breakdown",
            [nameof(ProgressController.GetWeakestWords)] = "weakest-words",
            [nameof(ProgressController.GetWordProgress)] = "words/{wordId:uint}",
        };
        foreach (var (methodName, template) in expectedTemplates)
        {
            typeof(ProgressController).GetMethod(methodName)!
                .GetCustomAttribute<HttpGetAttribute>()!.Template
                .Should().Be(template);
        }
    }

    [Fact]
    public void Controller_Should_Depend_Only_On_Bll_Services()
    {
        var constructor = typeof(ProgressController).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IProgressSummaryService), typeof(IProgressAnalyticsService));
    }

    [Fact]
    public void Bll_Services_Should_Depend_Only_On_Bll_Owned_Ports()
    {
        typeof(ProgressSummaryService).GetConstructors().Single()
            .GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IProgressSummaryRepository), typeof(IProgressSummaryCache));
        typeof(ProgressAnalyticsService).GetConstructors().Single()
            .GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IProgressAnalyticsRepository));
    }

    [Fact]
    public void Dal_Implementations_Should_Implement_Bll_Owned_Ports()
    {
        typeof(ProgressSummaryRepository).Should().Implement<IProgressSummaryRepository>();
        typeof(ProgressAnalyticsRepository).Should().Implement<IProgressAnalyticsRepository>();
        typeof(RedisProgressSummaryCache).Should().Implement<IProgressSummaryCache>();
    }

    [Fact]
    public void Progress_Controller_Source_Should_Not_Reference_Dal_Or_Ef()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "VocaNova.API",
            "Features",
            "Progress",
            "Controllers",
            "ProgressController.cs"));

        source.Should().NotContain("VocaNova.API.Features.Progress.DAL");
        source.Should().NotContain("VocaNovaDbContext");
        source.Should().NotContain("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Progress_Dal_Source_Should_Not_Reference_Http_Contracts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiRoot = Path.Combine(repositoryRoot, "src", "VocaNova.API");
        var progressFiles = Directory.EnumerateFiles(
                Path.Combine(apiRoot, "Features", "Progress", "DAL"),
                "*.cs",
                SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(
                Path.Combine(apiRoot, "Infrastructure", "Caching", "Progress"),
                "*.cs",
                SearchOption.AllDirectories));

        foreach (var file in progressFiles)
        {
            File.ReadAllText(file).Should().NotContain(
                "VocaNova.API.Features.Progress.Contracts",
                $"Progress DAL source {Path.GetRelativePath(repositoryRoot, file)} must not use HTTP contracts");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VocaNova.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
