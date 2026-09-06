using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using VocaNova.Dashboard.Controllers;
using VocaNova.Dashboard.Models.Auth;
using VocaNova.Dashboard.Services.Auth;

namespace VocaNova.Tests.Dashboard;

public sealed class DashboardForgotPasswordTests
{
    [Fact]
    public async Task ForgotPasswordPost_Should_Send_Otp_And_Redirect_To_Reset_Form()
    {
        var authService = new Mock<IDashboardAuthService>();
        authService
            .Setup(service => service.ForgotPasswordAsync("0912345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DashboardAuthActionResult.Ok("Password reset OTP sent successfully."));
        var controller = NewAuthController(authService.Object);

        var result = await controller.ForgotPassword(
            new ForgotPasswordViewModel { Phone = " 0912345678 " },
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(AuthController.ResetPassword));
        redirect.RouteValues!["phone"].Should().Be("0912345678");
    }

    [Fact]
    public async Task ResetPasswordPost_Should_Reset_And_Redirect_To_Login()
    {
        var authService = new Mock<IDashboardAuthService>();
        authService
            .Setup(service => service.ResetPasswordAsync(
                "0912345678",
                "123456",
                "NewPassword1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DashboardAuthActionResult.Ok("Password reset successfully."));
        var controller = NewAuthController(authService.Object);

        var result = await controller.ResetPassword(
            new ResetPasswordViewModel
            {
                Phone = "0912345678",
                OtpCode = " 123456 ",
                NewPassword = "NewPassword1",
                ConfirmPassword = "NewPassword1",
            },
            CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(AuthController.Login));
    }

    [Fact]
    public async Task ResetPasswordPost_Should_Show_Api_Error_When_Otp_Is_Invalid()
    {
        var authService = new Mock<IDashboardAuthService>();
        authService
            .Setup(service => service.ResetPasswordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DashboardAuthActionResult.Fail("Invalid or expired OTP."));
        var controller = NewAuthController(authService.Object);

        var result = await controller.ResetPassword(
            new ResetPasswordViewModel
            {
                Phone = "0912345678",
                OtpCode = "123456",
                NewPassword = "NewPassword1",
                ConfirmPassword = "NewPassword1",
            },
            CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Be("Invalid or expired OTP.");
    }

    private static AuthController NewAuthController(IDashboardAuthService authService)
    {
        var controller = new AuthController(authService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            Mock.Of<ITempDataProvider>());
        return controller;
    }
}
