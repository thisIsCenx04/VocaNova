using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ListsController = VocaNova.API.Features.Lists.Controllers.ListsController;
using PersonalTopicsController = VocaNova.API.Features.Lists.Controllers.PersonalTopicsController;

namespace VocaNova.Tests.Architecture;

public class ListQueryArchitectureTests
{
    [Fact]
    public void Controllers_Should_Preserve_Routes_Verbs_And_Authorization()
    {
        typeof(ListsController).GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/lists");
        typeof(PersonalTopicsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/personal-topics");
        typeof(ListsController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        typeof(PersonalTopicsController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        typeof(ListsController).GetMethod(nameof(ListsController.GetLists))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().BeNull();
        typeof(ListsController).GetMethod(nameof(ListsController.GetWords))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("{id:uint}/words");
        typeof(PersonalTopicsController).GetMethod(nameof(PersonalTopicsController.GetTopics))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().BeNull();
        typeof(PersonalTopicsController).GetMethod(nameof(PersonalTopicsController.GetWords))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("{topicId:uint}/words");
    }

    [Fact]
    public void Controllers_And_Services_Should_Use_Only_Bll_Boundaries()
    {
        typeof(ListsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IListQueryService), typeof(IListMutationService));
        typeof(PersonalTopicsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(
                typeof(IPersonalTopicQueryService),
                typeof(IPersonalTopicMutationService));
        typeof(ListQueryService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IListQueryRepository), typeof(IUserListCache));
        typeof(PersonalTopicQueryService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IPersonalTopicQueryRepository));
    }

    [Fact]
    public void Dal_Implementations_Should_Implement_Bll_Owned_Ports()
    {
        typeof(ListQueryRepository).Should().Implement<IListQueryRepository>();
        typeof(PersonalTopicQueryRepository).Should().Implement<IPersonalTopicQueryRepository>();
        typeof(RedisUserListCache).Should().Implement<IUserListCache>();
    }

    [Fact]
    public void Lists_Bll_Source_Should_Not_Reference_Outer_Layers_Or_Frameworks()
    {
        var repositoryRoot = FindRepositoryRoot();
        var bllRoot = Path.Combine(
            repositoryRoot,
            "src",
            "VocaNova.API",
            "Features",
            "Lists",
            "BLL");
        var files = Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories);
        var forbidden = new[]
        {
            "VocaNova.API.Features.Lists.DAL",
            "VocaNova.API.Infrastructure",
            "VocaNova.API.Features.Lists.Contracts",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "StackExchange.Redis",
            "VocaNovaDbContext",
        };

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            foreach (var reference in forbidden)
            {
                source.Should().NotContain(
                    reference,
                    $"Lists BLL source {Path.GetRelativePath(repositoryRoot, file)} must remain framework-neutral");
            }
        }
    }

    [Fact]
    public void Lists_Dal_Source_Should_Not_Reference_Http_Contracts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiRoot = Path.Combine(repositoryRoot, "src", "VocaNova.API");
        var files = Directory.EnumerateFiles(
                Path.Combine(apiRoot, "Features", "Lists", "DAL"),
                "*.cs",
                SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(
                Path.Combine(apiRoot, "Infrastructure", "Caching", "Lists"),
                "*.cs",
                SearchOption.AllDirectories));
        foreach (var file in files)
        {
            File.ReadAllText(file).Should().NotContain("VocaNova.API.Features.Lists.Contracts");
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
