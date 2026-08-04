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
