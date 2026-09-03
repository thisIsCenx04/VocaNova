using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VocaNova.Tests.Architecture;

public class NotificationArchitectureTests
{
    [Fact]
    public void Controller_Should_Preserve_Route_Verb_And_Authorization()
    {
        typeof(NotificationsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/notifications");
        typeof(NotificationsController).GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull();
        typeof(NotificationsController).GetMethod(nameof(NotificationsController.List))!
            .GetCustomAttribute<HttpGetAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void Controller_Should_Depend_Only_On_Bll_Service()
    {
        var constructor = typeof(NotificationsController).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(INotificationService));
        typeof(NotificationsController).IsSubclassOf(typeof(ControllerBase)).Should().BeTrue();
    }

    [Fact]
    public void Bll_Service_Should_Depend_On_Persistence_Abstraction()
    {
        var constructor = typeof(NotificationService).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(INotificationRepository));
    }

    [Fact]
    public void Dal_Repository_Should_Implement_Bll_Port()
    {
        typeof(NotificationRepository).Should().Implement<INotificationRepository>();
        typeof(NotificationRepository).Namespace.Should()
            .Be("VocaNova.API.Features.Notifications.DAL.Repositories");
    }

    [Fact]
    public void Bll_Source_Should_Not_Reference_Presentation_Dal_Or_Frameworks()
    {
        var repositoryRoot = FindRepositoryRoot();
        var bllRoot = Path.Combine(
            repositoryRoot,
            "src",
            "VocaNova.API",
            "Features",
            "Notifications",
            "BLL");
        var forbiddenReferences = new[]
        {
            "VocaNova.API.Features.Notifications.DAL",
            "VocaNova.API.Features.Notifications.Controllers",
            "VocaNova.API.Features.Notifications.Contracts",
            "VocaNova.API.Features.Notifications.Mappings",
            "VocaNova.API.Infrastructure",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
        };

        foreach (var file in Directory.EnumerateFiles(bllRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var forbiddenReference in forbiddenReferences)
            {
                source.Should().NotContain(
                    forbiddenReference,
                    $"BLL source {Path.GetRelativePath(repositoryRoot, file)} must remain framework and outer-layer independent");
            }
        }
    }

    [Fact]
    public void Notification_Controller_Source_Should_Not_Reference_Dal_Or_Ef()
    {
        var repositoryRoot = FindRepositoryRoot();
        var controllerPath = Path.Combine(
            repositoryRoot,
            "src",
            "VocaNova.API",
            "Features",
            "Notifications",
            "Controllers",
            "NotificationsController.cs");
        var source = File.ReadAllText(controllerPath);

        source.Should().NotContain("VocaNova.API.Features.Notifications.DAL");
        source.Should().NotContain("VocaNovaDbContext");
        source.Should().NotContain("Microsoft.EntityFrameworkCore");
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
