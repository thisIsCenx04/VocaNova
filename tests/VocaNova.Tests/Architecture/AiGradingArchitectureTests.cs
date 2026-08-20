using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace VocaNova.Tests.Architecture;

public sealed class AiGradingArchitectureTests
{
    [Fact]
    public void Controller_Should_Preserve_Routes_Authorization_And_Verbs()
    {
        typeof(AdminAiGradingController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/admin/settings/ai-grading");
        typeof(AdminAiGradingController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be(JwtAuthenticationExtensions.AdminPolicy);
        AssertRoute(nameof(AdminAiGradingController.GetConfig), typeof(HttpGetAttribute), null);
        AssertRoute(nameof(AdminAiGradingController.UpdateConfig), typeof(HttpPutAttribute), null);
        AssertRoute(nameof(AdminAiGradingController.ResetConfig), typeof(HttpPostAttribute), "reset");
        AssertRoute(nameof(AdminAiGradingController.TestConnection), typeof(HttpPostAttribute), "test");
    }

    [Fact]
    public void Controller_And_Adapters_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(AdminAiGradingController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IAiGradingConfigurationService), typeof(IAiGradingProvider));
        typeof(AiGradingCacheRepository).Should().Implement<IAiGradingCacheRepository>();
        typeof(GeminiAiGradingProvider).Should().Implement<IAiGradingProvider>();
    }

    [Fact]
    public void Bll_Source_Should_Not_Reference_Http_Ef_Dal_Or_Infrastructure()
    {
        var root = FindRepositoryRoot();
        var bllRoot = Path.Combine(root, "src", "VocaNova.API", "Features", "AiGrading", "BLL");
        var forbidden = new[]
        {
            "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore",
            "VocaNova.API.Features.AiGrading.Contracts", "VocaNova.API.Features.AiGrading.DAL",
            "VocaNova.API.Infrastructure", "StackExchange.Redis", "VocaNovaDbContext",
        };
        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        foreach (var token in forbidden)
            File.ReadAllText(file).Should().NotContain(token,
                $"AI-grading BLL source {Path.GetRelativePath(root, file)} must remain boundary-independent");
    }

    private static void AssertRoute(string methodName, Type attributeType, string? template)
    {
        var attribute = typeof(AdminAiGradingController).GetMethod(methodName)!
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
