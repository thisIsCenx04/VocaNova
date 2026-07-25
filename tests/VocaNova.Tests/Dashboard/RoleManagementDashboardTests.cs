using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VocaNova.Dashboard.Controllers;
using VocaNova.Dashboard.Models.Api.SuperAdmin;
using VocaNova.Dashboard.Models.Roles;
using VocaNova.Dashboard.Services.Api;
using VocaNova.Dashboard.Services.Localization;

namespace VocaNova.Tests.Dashboard;

public sealed class RoleManagementDashboardTests
{
    [Fact]
    public void RolesController_Should_Require_SuperAdmin()
    {
        var attribute = typeof(RolesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();

        attribute.Roles.Should().Be("super_admin");
    }

    [Fact]
    public async Task EditGet_Should_Navigate_To_Separate_Edit_View()
    {
        var client = new Mock<IVocaNovaApiClient>();
        client.Setup(item => item.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedData<ManagedRole>
            {
                Items = [new ManagedRole(4, "content_manager")],
                Page = 1, Limit = 100, TotalItems = 1, TotalPages = 1,
            });
        var controller = new RolesController(client.Object);

        var result = await controller.Edit(4, CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<SaveRoleViewModel>()
            .Which.RoleName.Should().Be("content_manager");
    }

    [Fact]
    public async Task Assignments_Should_Show_Thirty_Users_Per_Page()
    {
        var users = Enumerable.Range(1, 31)
            .Select(id => new AssignmentUser((uint)id, $"User {id:00}", $"{id}@example.com", null))
            .ToArray();
        var overview = new AdminUserAssignmentOverview(
            [new AssignmentAdmin(20, "Admin A", "admin@example.com")],
            users);
        var client = new Mock<IVocaNovaApiClient>();
        client.Setup(item => item.GetAdminUserAssignmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);
        var controller = new RolesController(client.Object);

        var firstResult = await controller.Assignments(20, null, null, 1, CancellationToken.None);
        var firstPage = firstResult.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeOfType<AdminUserAssignmentViewModel>().Subject;
        firstPage.Users.Should().HaveCount(30);

        var secondResult = await controller.Assignments(20, null, null, 2, CancellationToken.None);
        var secondPage = secondResult.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeOfType<AdminUserAssignmentViewModel>().Subject;
        secondPage.Users.Should().ContainSingle();
        secondPage.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Assignments_Should_Filter_By_Search_And_Assignment_Status()
    {
        var overview = new AdminUserAssignmentOverview(
            [
                new AssignmentAdmin(20, "Admin A", "admin-a@example.com"),
                new AssignmentAdmin(21, "Admin B", "admin-b@example.com"),
            ],
            [
                new AssignmentUser(1, "An", "an@example.com", 20),
                new AssignmentUser(2, "Binh", "binh@example.com", 21),
                new AssignmentUser(3, "Chi", "chi@example.com", null),
            ]);
        var client = new Mock<IVocaNovaApiClient>();
        client.Setup(item => item.GetAdminUserAssignmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);
        var controller = new RolesController(client.Object);

        var result = await controller.Assignments(20, "binh", "other", 1, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeOfType<AdminUserAssignmentViewModel>().Subject;
        model.Users.Select(user => user.UserId).Should().Equal(2u);
        model.TotalUsers.Should().Be(1);
    }

    [Theory]
    [InlineData("vi", "Quản lý vai trò")]
    [InlineData("en", "Role Management")]
    public void Translator_Should_Localize_Role_Management(string language, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{Translator.LanguageCookie}={language}";
        var translator = new Translator(new HttpContextAccessor { HttpContext = context });

        translator["Role Management"].Should().Be(expected);
    }
}
