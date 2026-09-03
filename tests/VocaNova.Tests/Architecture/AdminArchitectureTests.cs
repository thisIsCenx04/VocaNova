using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Services;
using VocaNova.API.Features.Admin.Controllers;
using VocaNova.API.Features.Admin.DAL.Repositories;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.Tests.Architecture;

public sealed class AdminArchitectureTests
{
    [Fact]
    public void Controllers_Should_Preserve_Admin_Routes_Authorization_And_Verbs()
    {
        typeof(AdminStatsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/admin");
        typeof(AdminUsersController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/admin/users");

        typeof(AdminStatsController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be(JwtAuthenticationExtensions.AdminPolicy);
        typeof(AdminUsersController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be(JwtAuthenticationExtensions.AdminPolicy);

        AssertRoute<AdminStatsController>(nameof(AdminStatsController.GetDashboard), typeof(HttpGetAttribute), "stats/dashboard");
        AssertRoute<AdminStatsController>(nameof(AdminStatsController.GetDemographics), typeof(HttpGetAttribute), "stats/demographics");
        AssertRoute<AdminStatsController>(nameof(AdminStatsController.GetLearningStats), typeof(HttpGetAttribute), "stats/learning");
        AssertRoute<AdminStatsController>(nameof(AdminStatsController.GetSessionsTrend), typeof(HttpGetAttribute), "stats/sessions-trend");
        AssertRoute<AdminStatsController>(nameof(AdminStatsController.GetMasteryDistribution), typeof(HttpGetAttribute), "stats/mastery-distribution");
        AssertRoute<AdminStatsController>(nameof(AdminStatsController.GetActivityTrend), typeof(HttpGetAttribute), "stats/activity-trend");
        AssertRoute<AdminStatsController>(nameof(AdminStatsController.GetAuditLogs), typeof(HttpGetAttribute), "audit-logs");

        AssertRoute<AdminUsersController>(nameof(AdminUsersController.GetUsers), typeof(HttpGetAttribute), null);
        AssertRoute<AdminUsersController>(nameof(AdminUsersController.GetUserDetail), typeof(HttpGetAttribute), "{id:uint}");
        AssertRoute<AdminUsersController>(nameof(AdminUsersController.GetTestHistory), typeof(HttpGetAttribute), "{id:uint}/test-history");
        AssertRoute<AdminUsersController>(nameof(AdminUsersController.GetTopics), typeof(HttpGetAttribute), "{id:uint}/topics");
        AssertRoute<AdminUsersController>(nameof(AdminUsersController.Deactivate), typeof(HttpPatchAttribute), "{id:uint}/deactivate");
        AssertRoute<AdminUsersController>(nameof(AdminUsersController.Restore), typeof(HttpPatchAttribute), "{id:uint}/restore");
    }

    [Fact]
    public void Controllers_And_Dal_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(AdminStatsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Equal(typeof(IAdminStatsService));
        typeof(AdminUsersController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Equal(typeof(IAdminUserService));

        typeof(AdminStatsService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Contain(typeof(IAdminStatsRepository));
        typeof(AdminUserService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Contain(typeof(IAdminUserRepository));

        typeof(AdminStatsRepository).Should().Implement<IAdminStatsRepository>();
        typeof(AdminUserRepository).Should().Implement<IAdminUserRepository>();
    }

    [Fact]
    public void Admin_Bll_Source_Should_Not_Reference_Http_Ef_Dal_Or_Infrastructure()
    {
        var root = FindRepositoryRoot();
        var bllRoot = Path.Combine(root, "src", "VocaNova.API", "Features", "Admin", "BLL");
        var forbidden = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "VocaNova.API.Features.Admin.Contracts",
            "VocaNova.API.Features.Admin.DAL",
            "VocaNova.API.Infrastructure",
            "StackExchange.Redis",
            "VocaNovaDbContext",
            "StatusCodes",
        };

        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                source.Should().NotContain(
                    token,
                    $"Admin BLL source {Path.GetRelativePath(root, file)} must remain framework-neutral");
            }
        }
    }

    private static void AssertRoute<TController>(string methodName, Type attributeType, string? template)
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
