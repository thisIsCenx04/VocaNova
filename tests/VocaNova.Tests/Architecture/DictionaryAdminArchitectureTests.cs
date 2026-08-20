using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace VocaNova.Tests.Architecture;

public sealed class DictionaryAdminArchitectureTests
{
    [Fact]
    public void Controllers_Should_Preserve_Admin_Routes_Authorization_And_Verbs()
    {
        typeof(AdminWordsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/admin/words");
        typeof(AdminTopicsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/admin/topics");
        typeof(AdminWordsController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be(JwtAuthenticationExtensions.AdminPolicy);
        typeof(AdminTopicsController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be(JwtAuthenticationExtensions.AdminPolicy);

        AssertRoute<AdminWordsController>(nameof(AdminWordsController.List), typeof(HttpGetAttribute), null);
        AssertRoute<AdminWordsController>(nameof(AdminWordsController.Create), typeof(HttpPostAttribute), null);
        AssertRoute<AdminWordsController>(nameof(AdminWordsController.Update), typeof(HttpPutAttribute), "{id:uint}");
        AssertRoute<AdminWordsController>(nameof(AdminWordsController.SoftDelete), typeof(HttpDeleteAttribute), "{id:uint}");
        AssertRoute<AdminWordsController>(nameof(AdminWordsController.Restore), typeof(HttpPatchAttribute), "{id:uint}/restore");
        AssertRoute<AdminWordsController>(nameof(AdminWordsController.SoftDeleteSense), typeof(HttpDeleteAttribute), "{id:uint}/senses/{senseId:uint}");
        AssertRoute<AdminWordsController>(nameof(AdminWordsController.RestoreSense), typeof(HttpPatchAttribute), "{id:uint}/senses/{senseId:uint}/restore");
        AssertRoute<AdminTopicsController>(nameof(AdminTopicsController.List), typeof(HttpGetAttribute), null);
        AssertRoute<AdminTopicsController>(nameof(AdminTopicsController.AddWords), typeof(HttpPostAttribute), "{id:uint}/words");
    }

    [Fact]
    public void Controllers_And_Dal_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(AdminWordsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Equal(typeof(IWordAdminService));
        typeof(AdminTopicsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Equal(typeof(ITopicAdminService));
        typeof(WordAdminRepository).Should().Implement<IWordAdminRepository>();
        typeof(TopicAdminRepository).Should().Implement<ITopicAdminRepository>();
        typeof(CloudinaryWordAudioStorage).Should().Implement<IWordAudioStorage>();
        typeof(CloudinaryWordImageStorage).Should().Implement<IWordImageStorage>();
    }

    [Fact]
    public void Dictionary_Admin_Bll_Source_Should_Not_Reference_Http_Ef_Dal_Or_Infrastructure()
    {
        var root = FindRepositoryRoot();
        var bllRoot = Path.Combine(root, "src", "VocaNova.API", "Features", "Dictionary", "BLL");
        var forbidden = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "VocaNova.API.Features.Dictionary.Contracts",
            "VocaNova.API.Features.Dictionary.DAL",
            "VocaNova.API.Infrastructure",
        };

        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                source.Should().NotContain(token,
                    $"Dictionary BLL source {Path.GetRelativePath(root, file)} must remain framework-neutral");
            }
        }
    }

    private static void AssertRoute<TController>(string methodName, Type attributeType, string? template)
    {
        var attribute = typeof(TController).GetMethod(methodName)!
            .GetCustomAttributes(attributeType, false).Cast<HttpMethodAttribute>().Single();
        attribute.Template.Should().Be(template);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VocaNova.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
