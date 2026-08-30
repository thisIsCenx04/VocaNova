using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VocaNova.Dashboard.Controllers;
using VocaNova.Dashboard.Models.AdminAccounts;
using VocaNova.Dashboard.Data.Dtos.SuperAdmin;
using VocaNova.Dashboard.Data.Dtos.Users;
using VocaNova.Dashboard.Models.Auth;
using VocaNova.Dashboard.Services.Api;
using VocaNova.Dashboard.Services.Auth;
using VocaNova.Dashboard.Services.Localization;

namespace VocaNova.Tests.Dashboard;

public sealed class SuperAdminDashboardTests
{
    [Fact]
    public async Task LoginPost_Should_Redirect_SuperAdmin_To_AdminAccounts_Dashboard()
    {
        var authService = new Mock<IDashboardAuthService>();
        authService
            .Setup(service => service.LoginAsync("0912345678", "Strong123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DashboardAuthResult.Success(
                new DashboardUser(1, "0912345678", "Root", null, "super_admin", "active"),
                "access", "refresh", 900));
        var authentication = new Mock<IAuthenticationService>();
        authentication
            .Setup(service => service.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection()
            .AddSingleton(authentication.Object)
            .BuildServiceProvider();
        var controller = new AuthController(authService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } },
            Url = Mock.Of<IUrlHelper>(),
        };

        var result = await controller.Login(
            new LoginViewModel { Phone = "0912345678", Password = "Strong123" },
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ControllerName.Should().Be("AdminAccounts");
        redirect.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task LoginPost_Should_Keep_Normal_Admin_On_Overview_Dashboard()
    {
        var authService = new Mock<IDashboardAuthService>();
        authService
            .Setup(service => service.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DashboardAuthResult.Success(
                new DashboardUser(2, "0987654321", "Admin", null, "admin", "active"),
                "access", "refresh", 900));
        var authentication = new Mock<IAuthenticationService>();
        authentication
            .Setup(service => service.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var controller = new AuthController(authService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } },
            Url = Mock.Of<IUrlHelper>(),
        };

        var result = await controller.Login(
            new LoginViewModel { Phone = "0987654321", Password = "Strong123" },
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ControllerName.Should().Be("Dashboard");
        redirect.ActionName.Should().Be("Index");
    }

    [Fact]
    public void AdminAccountsController_Should_Require_SuperAdmin_Role()
    {
        var attribute = typeof(AdminAccountsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        attribute.Roles.Should().Be("super_admin");
    }

    [Fact]
    public async Task Index_Should_Load_Admin_Accounts_From_SuperAdmin_Api_Client()
    {
        var apiClient = new Mock<IVocaNovaApiClient>();
        apiClient
            .Setup(client => client.GetAdminAccountsAsync(
                It.Is<AdminAccountFilter>(filter =>
                    filter.Status == "active" && filter.Search == "root" && !filter.IncludeDeleted
                    && filter.Page == 2 && filter.Limit == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedData<AdminAccount>
            {
                Items = [new AdminAccount(5, "Root Admin", "root@example.com", "0912345678", "admin", "active", DateTime.UtcNow, DateTime.UtcNow, null)],
                Page = 2,
                Limit = 10,
                TotalItems = 11,
                TotalPages = 2,
            });
        var controller = NewAdminAccountsController(apiClient.Object);

        var result = await controller.Index(
            " root ",
            "ACTIVE",
            includeDeleted: false,
            page: 2,
            cancellationToken: CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<AdminAccountListViewModel>().Subject;
        model.Items.Should().ContainSingle(item => item.AdminId == 5);
        model.Search.Should().Be("root");
        model.Status.Should().Be("active");
    }

    [Theory]
    [InlineData("vi", "Tài khoản quản trị")]
    [InlineData("en", "Admin Accounts")]
    public void Translator_Should_Render_Admin_Accounts_In_Selected_Language(string language, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{Translator.LanguageCookie}={language}";
        var translator = new Translator(new HttpContextAccessor { HttpContext = context });

        translator["Admin Accounts"].Should().Be(expected);
        translator["Add Admin Account"].Should().Be(
            language == "vi" ? "Thêm tài khoản quản trị" : "Add Admin Account");
    }

    [Fact]
    public async Task Create_Should_Send_Form_Data_To_SuperAdmin_Api()
    {
        var apiClient = new Mock<IVocaNovaApiClient>();
        apiClient
            .Setup(client => client.CreateAdminAccountAsync(It.IsAny<AdminAccountInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiActionResult.Ok(StatusCodes.Status201Created));
        var controller = NewAdminAccountsController(apiClient.Object);
        var model = new CreateAdminAccountViewModel
        {
            FullName = "New Admin",
            Email = "new@example.com",
            Phone = "0934567890",
            Password = "Strong123",
            Status = "active",
        };

        var result = await controller.Create(model, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        apiClient.Verify(client => client.CreateAdminAccountAsync(
            It.Is<AdminAccountInput>(input =>
                input.FullName == "New Admin"
                && input.Email == "new@example.com"
                && input.Phone == "0934567890"
                && input.Password == "Strong123"
                && input.Status == "active"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleAssignment_Should_Load_User_Accounts_In_Pages_Of_Thirty()
    {
        var apiClient = new Mock<IVocaNovaApiClient>();
        apiClient.Setup(client => client.GetUsersAsync(
                It.Is<UserListFilter>(filter =>
                    filter.Search == "an" && filter.Role == "user"
                    && filter.Page == 2 && filter.Limit == 30),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedData<AdminUserSummary>
            {
                Items =
                [
                    new AdminUserSummary(31, null, "an@example.com", "An", null, "user",
                        "active", null, DateTime.UtcNow),
                ],
                Page = 2,
                Limit = 30,
                TotalItems = 31,
                TotalPages = 2,
            });
        var controller = NewAdminAccountsController(apiClient.Object);

        var result = await controller.RoleAssignment(" an ", "user", 2, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeOfType<AccountRoleAssignmentViewModel>().Subject;
        model.Items.Should().ContainSingle(item => item.UserId == 31);
        model.Limit.Should().Be(30);
        model.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task ChangeRole_Should_Assign_Admin_Role_To_User()
    {
        var apiClient = new Mock<IVocaNovaApiClient>();
        apiClient.Setup(client => client.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedData<ManagedRole>
            {
                Items = [new ManagedRole(1, "user"), new ManagedRole(2, "admin")],
                Page = 1,
                Limit = 100,
                TotalItems = 2,
                TotalPages = 1,
            });
        apiClient.Setup(client => client.AssignRoleAsync(2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiActionResult.Ok(StatusCodes.Status200OK));
        var controller = NewAdminAccountsController(apiClient.Object);

        var result = await controller.ChangeRole(10, "admin", null, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("RoleAssignment");
        apiClient.Verify(client => client.AssignRoleAsync(2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AdminAccountsController NewAdminAccountsController(IVocaNovaApiClient apiClient)
    {
        var controller = new AdminAccountsController(apiClient)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            Mock.Of<ITempDataProvider>());
        return controller;
    }
}
