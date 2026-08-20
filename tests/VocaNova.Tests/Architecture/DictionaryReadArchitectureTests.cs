using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Tests.Architecture;

public class DictionaryReadArchitectureTests
{
    [Fact]
    public void Controllers_Should_Preserve_Public_Routes_And_Anonymous_Access()
    {
        typeof(WordsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/words");
        typeof(TopicsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/topics");
        typeof(WordsController).GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
        typeof(TopicsController).GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();

        typeof(WordsController).GetMethod(nameof(WordsController.Search))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().BeNull();
        typeof(WordsController).GetMethod(nameof(WordsController.GetById))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("{id:uint}");
        typeof(WordsController).GetMethod(nameof(WordsController.GetDaily))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("daily");
        typeof(TopicsController).GetMethod(nameof(TopicsController.GetTopics))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().BeNull();
        typeof(TopicsController).GetMethod(nameof(TopicsController.GetWords))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("{id:uint}/words");
    }

    [Fact]
    public void Controllers_Should_Depend_Only_On_Bll_Read_Services()
    {
        typeof(WordsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IWordReadService));
        typeof(TopicsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(ITopicReadService));
    }

    [Fact]
    public void Bll_Read_Services_Should_Depend_Only_On_Bll_Owned_Ports()
    {
        typeof(WordReadService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(
                typeof(IWordReadRepository),
                typeof(IWordSearchCache),
                typeof(IWordDetailCache));
        typeof(TopicReadService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(
                typeof(ITopicReadRepository),
                typeof(IWordReadRepository),
                typeof(ITopicCache));
    }

    [Fact]
    public void Dal_Implementations_Should_Implement_Bll_Owned_Ports()
    {
        typeof(WordReadRepository).Should().Implement<IWordReadRepository>();
        typeof(TopicReadRepository).Should().Implement<ITopicReadRepository>();
        typeof(RedisWordSearchCache).Should().Implement<IWordSearchCache>();
        typeof(RedisWordDetailCache).Should().Implement<IWordDetailCache>();
        typeof(RedisTopicCache).Should().Implement<ITopicCache>();
    }

    [Fact]
    public void Dictionary_Read_Source_Should_Respect_Layer_Boundaries()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiRoot = Path.Combine(repositoryRoot, "src", "VocaNova.API");
        var controllerRoot = Path.Combine(apiRoot, "Features", "Dictionary", "Controllers");
        var dalRoot = Path.Combine(apiRoot, "Features", "Dictionary", "DAL");

        foreach (var file in new[]
                 {
                     Path.Combine(controllerRoot, "WordsController.cs"),
                     Path.Combine(controllerRoot, "TopicsController.cs"),
                 })
        {
            var source = File.ReadAllText(file);
            source.Should().NotContain("VocaNova.API.Features.Dictionary.DAL");
            source.Should().NotContain("VocaNovaDbContext");
            source.Should().NotContain("Microsoft.EntityFrameworkCore");
        }

        var dalFiles = Directory.EnumerateFiles(dalRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(
                Path.Combine(apiRoot, "Infrastructure", "Caching", "Dictionary"),
                "*.cs",
                SearchOption.AllDirectories));
        foreach (var file in dalFiles)
        {
            File.ReadAllText(file).Should().NotContain(
                "VocaNova.API.Features.Dictionary.Contracts",
                $"Dictionary DAL source {Path.GetRelativePath(repositoryRoot, file)} must not use HTTP contracts");
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
