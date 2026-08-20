using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Hosting;

namespace VocaNova.Tests.Architecture;

public sealed class KnnArchitectureTests
{
    [Fact]
    public void Public_Recommendation_Controller_Should_Preserve_Routes_Authorization_And_Verbs()
    {
        typeof(RecommendationsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/recommendations");
        typeof(RecommendationsController).GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull();

        AssertRoute<RecommendationsController>(nameof(RecommendationsController.GetTopics), typeof(HttpGetAttribute), "topics");
        AssertRoute<RecommendationsController>(nameof(RecommendationsController.GetWords), typeof(HttpGetAttribute), "words");
        AssertRoute<RecommendationsController>(nameof(RecommendationsController.GetPersonalTopics), typeof(HttpGetAttribute), "personal-topics");
        AssertRoute<RecommendationsController>(nameof(RecommendationsController.GetLearningProfileOptions), typeof(HttpGetAttribute), "learning-profile-options");
        AssertRoute<RecommendationsController>(nameof(RecommendationsController.SelectTopics), typeof(HttpPutAttribute), "topics/selection");
        AssertRoute<RecommendationsController>(nameof(RecommendationsController.AcceptTopic), typeof(HttpPostAttribute), "topics/{topicId:uint}/accept");
    }

    [Fact]
    public void Admin_Knn_Controller_Should_Preserve_Target_Admin_Routes()
    {
        typeof(AdminKnnController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/admin/knn");
        typeof(AdminKnnController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be("Admin");

        AssertRoute<AdminKnnController>(nameof(AdminKnnController.GetConfig), typeof(HttpGetAttribute), "config");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.UpdateVectorWeights), typeof(HttpPutAttribute), "config/vector-weights");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.ResetVectorWeights), typeof(HttpPostAttribute), "config/vector-weights/reset");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.GetRebuildStatus), typeof(HttpGetAttribute), "rebuild-status");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.TriggerRebuild), typeof(HttpPostAttribute), "trigger-rebuild");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.GetAgeRanges), typeof(HttpGetAttribute), "age-ranges");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.CreateAgeRange), typeof(HttpPostAttribute), "age-ranges");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.UpdateAgeRange), typeof(HttpPutAttribute), "age-ranges/{id:uint}");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.DeleteAgeRange), typeof(HttpDeleteAttribute), "age-ranges/{id:uint}");
        AssertRoute<AdminKnnController>(nameof(AdminKnnController.RestoreAgeRange), typeof(HttpPatchAttribute), "age-ranges/{id:uint}/restore");
    }

    [Fact]
    public void Controllers_Dal_Cache_And_Hosted_Service_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(RecommendationsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IKnnOnboardingService), typeof(IKnnLearningService));

        typeof(AdminKnnController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Should().ContainInOrder(
                typeof(IAdminKnnLookupService),
                typeof(IKnnRebuildService),
                typeof(IKnnRuntimeConfigurationService),
                typeof(IAdminKnnTriggerRateLimiter));

        typeof(KnnProfileRepository).Should().Implement<IKnnProfileRepository>();
        typeof(KnnLearningRepository).Should().Implement<IKnnLearningRepository>();
        typeof(AdminKnnLookupRepository).Should().Implement<IAdminKnnLookupRepository>();
        typeof(RedisKnnTopicRecommendationCache).Should().Implement<IKnnTopicRecommendationCache>();
        typeof(RedisKnnWordRecommendationCache).Should().Implement<IKnnWordRecommendationCache>();
        typeof(RedisKnnRebuildStateCache).Should().Implement<IKnnRebuildStateCache>();
        typeof(InMemoryAdminKnnTriggerRateLimiter).Should().Implement<IAdminKnnTriggerRateLimiter>();
        typeof(KnnWordRecommendationJob).Should().BeAssignableTo<BackgroundService>();
    }

    [Fact]
    public void Bll_Source_Should_Not_Reference_Http_Ef_Dal_Redis_Or_Infrastructure()
    {
        var root = FindRepositoryRoot();
        var bllRoot = Path.Combine(root, "src", "VocaNova.API", "Features", "Knn", "BLL");
        var forbidden = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "VocaNova.API.Features.Knn.Contracts",
            "VocaNova.API.Features.Knn.DAL",
            "VocaNova.API.Infrastructure",
            "StackExchange.Redis",
            "VocaNovaDbContext",
        };

        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        foreach (var token in forbidden)
        {
            File.ReadAllText(file).Should().NotContain(
                token,
                $"KNN BLL source {Path.GetRelativePath(root, file)} must remain boundary-independent");
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
