using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Services;
using VocaNova.API.Features.SuperAdmin.Controllers;
using VocaNova.API.Features.SuperAdmin.DAL.Repositories;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.Tests.Architecture;

public sealed class SuperAdminArchitectureTests
{
    [Fact]
    public void Controllers_Should_Preserve_SuperAdmin_Routes_Authorization_And_Verbs()
    {
        typeof(SuperAdminAccountsController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/superadmin/admins");
        typeof(RolesController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/superadmin/roles");

        typeof(SuperAdminAccountsController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be(JwtAuthenticationExtensions.SuperAdminPolicy);
        typeof(RolesController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .Should().Be(JwtAuthenticationExtensions.SuperAdminPolicy);

        AssertRoute<SuperAdminAccountsController>(nameof(SuperAdminAccountsController.List), typeof(HttpGetAttribute), null);
        AssertRoute<SuperAdminAccountsController>(nameof(SuperAdminAccountsController.Detail), typeof(HttpGetAttribute), "{id:uint}");
        AssertRoute<SuperAdminAccountsController>(nameof(SuperAdminAccountsController.Create), typeof(HttpPostAttribute), null);
        AssertRoute<SuperAdminAccountsController>(nameof(SuperAdminAccountsController.Update), typeof(HttpPutAttribute), "{id:uint}");
        AssertRoute<SuperAdminAccountsController>(nameof(SuperAdminAccountsController.Lock), typeof(HttpPatchAttribute), "{id:uint}/lock");
        AssertRoute<SuperAdminAccountsController>(nameof(SuperAdminAccountsController.Unlock), typeof(HttpPatchAttribute), "{id:uint}/unlock");
        AssertRoute<SuperAdminAccountsController>(nameof(SuperAdminAccountsController.Delete), typeof(HttpDeleteAttribute), "{id:uint}");

        AssertRoute<RolesController>(nameof(RolesController.List), typeof(HttpGetAttribute), null);
        AssertRoute<RolesController>(nameof(RolesController.Create), typeof(HttpPostAttribute), null);
        AssertRoute<RolesController>(nameof(RolesController.Update), typeof(HttpPutAttribute), "{roleId:uint}");
        AssertRoute<RolesController>(nameof(RolesController.Delete), typeof(HttpDeleteAttribute), "{roleId:uint}");
        AssertRoute<RolesController>(nameof(RolesController.Users), typeof(HttpGetAttribute), "{roleId:uint}/users");
        AssertRoute<RolesController>(nameof(RolesController.Assign), typeof(HttpPostAttribute), "{roleId:uint}/users/{userId:uint}");
        AssertRoute<RolesController>(nameof(RolesController.Remove), typeof(HttpDeleteAttribute), "{roleId:uint}/users/{userId:uint}");
    }

    [Fact]
    public void Controllers_And_Dal_Should_Use_Bll_Owned_Boundaries()
    {
        typeof(SuperAdminAccountsController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Equal(typeof(ISuperAdminAccountService));
        typeof(RolesController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Equal(typeof(IRoleManagementService));

        typeof(SuperAdminAccountService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Contain(typeof(ISuperAdminAccountRepository));
        typeof(RoleManagementService).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType).Should().Contain(typeof(IRoleManagementRepository));

        typeof(SuperAdminAccountRepository).Should().Implement<ISuperAdminAccountRepository>();
        typeof(RoleManagementRepository).Should().Implement<IRoleManagementRepository>();
    }

    [Fact]
    public void SuperAdmin_Bll_Source_Should_Not_Reference_Http_Ef_Dal_Or_Infrastructure()
    {
        var root = FindRepositoryRoot();
        var bllRoot = Path.Combine(root, "src", "VocaNova.API", "Features", "SuperAdmin", "BLL");
        var forbidden = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "VocaNova.API.Features.SuperAdmin.Contracts",
            "VocaNova.API.Features.SuperAdmin.DAL",
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
                    $"SuperAdmin BLL source {Path.GetRelativePath(root, file)} must remain framework-neutral");
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
