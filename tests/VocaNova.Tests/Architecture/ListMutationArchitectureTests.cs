using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using ListsController = VocaNova.API.Features.Lists.Controllers.ListsController;
using PersonalTopicsController = VocaNova.API.Features.Lists.Controllers.PersonalTopicsController;

namespace VocaNova.Tests.Architecture;

public sealed class ListMutationArchitectureTests
{
    [Fact]
    public void Controllers_Should_Preserve_Mutation_Routes_And_Verbs()
    {
        AssertRoute<ListsController>(nameof(ListsController.Create), typeof(HttpPostAttribute), null);
        AssertRoute<ListsController>(nameof(ListsController.Update), typeof(HttpPutAttribute), "{id:uint}");
        AssertRoute<ListsController>(nameof(ListsController.SoftDelete), typeof(HttpDeleteAttribute), "{id:uint}");
        AssertRoute<ListsController>(nameof(ListsController.AddWord), typeof(HttpPostAttribute), "{id:uint}/words");
        AssertRoute<ListsController>(nameof(ListsController.AddRandomWords), typeof(HttpPostAttribute), "{id:uint}/words/random");
        AssertRoute<ListsController>(nameof(ListsController.RemoveWord), typeof(HttpDeleteAttribute), "{id:uint}/words/{wordId:uint}");
        AssertRoute<ListsController>(nameof(ListsController.UpdateWordNote), typeof(HttpPatchAttribute), "{id:uint}/words/{wordId:uint}/note");
        AssertRoute<PersonalTopicsController>(nameof(PersonalTopicsController.AddWord), typeof(HttpPostAttribute), "{topicId:uint}/words");
        AssertRoute<PersonalTopicsController>(nameof(PersonalTopicsController.RemoveWord), typeof(HttpDeleteAttribute), "{topicId:uint}/words/{wordId:uint}");
    }

    [Fact]
    public void Mutation_Services_And_Dal_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(ListMutationService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IListMutationRepository), typeof(IUserListCache));
        typeof(PersonalTopicMutationService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(
                typeof(IPersonalTopicMutationRepository),
                typeof(IListMutationRepository),
                typeof(IUserListCache));
        typeof(ListMutationRepository).Should().Implement<IListMutationRepository>();
        typeof(PersonalTopicMutationRepository).Should().Implement<IPersonalTopicMutationRepository>();
    }

    [Fact]
    public void Mutation_Ports_Should_Not_Expose_Http_Or_Ef_Types()
    {
        var forbiddenNamespaces = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "VocaNova.API.Features.Lists.Contracts",
            "VocaNova.API.Features.Lists.DAL",
            "VocaNova.API.Infrastructure",
        };
        foreach (var type in new[] { typeof(IListMutationRepository), typeof(IPersonalTopicMutationRepository) })
        {
            foreach (var method in type.GetMethods())
            {
                var signatureTypes = method.GetParameters().Select(parameter => parameter.ParameterType)
                    .Append(method.ReturnType);
                signatureTypes.Select(item => item.ToString()).Should().OnlyContain(
                    name => forbiddenNamespaces.All(forbidden => !name.Contains(forbidden, StringComparison.Ordinal)));
            }
        }
    }

    [Fact]
    public void Unit2_Should_Not_Introduce_A_Transaction_Or_Atomic_Wrapper()
    {
        var root = FindRepositoryRoot();
        var files = Directory.EnumerateFiles(
                Path.Combine(root, "src", "VocaNova.API"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => path.Contains("Lists", StringComparison.OrdinalIgnoreCase))
            .Where(path => path.Contains("BLL", StringComparison.OrdinalIgnoreCase)
                || path.Contains("DAL", StringComparison.OrdinalIgnoreCase));
        var forbidden = new[]
        {
            "BeginTransaction",
            "TransactionScope",
            "IApplicationTransaction",
            "CreateExecutionStrategy",
            "AtomicallyAsync",
        };
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                source.Should().NotContain(token);
            }
        }
    }

    private static void AssertRoute<TController>(
        string methodName,
        Type attributeType,
        string? template)
    {
        var attribute = typeof(TController).GetMethod(methodName)!
            .GetCustomAttributes(attributeType, false)
            .Cast<HttpMethodAttribute>()
            .Single();
        attribute.Template.Should().Be(template);
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
