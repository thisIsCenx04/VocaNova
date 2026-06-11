using FluentAssertions;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Validators;

namespace VocaNova.Tests.Auth;

public class AuthValidatorTests
{
    [Fact]
    public void RegisterRequestValidator_Should_Accept_Valid_Request()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest("0912345678", "Password1", "Nguyen Van A"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterRequestValidator_Should_Reject_Invalid_Request()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest("0212345678", "weak", "A"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Phone));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Password));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.DisplayName));
    }

    [Fact]
    public void LoginRequestValidator_Should_Reject_Missing_Password()
    {
        var validator = new LoginRequestValidator();

        var result = validator.Validate(new LoginRequest("0912345678", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void OtpSendRequestValidator_Should_Validate_Phone()
    {
        var validator = new OtpSendRequestValidator();

        var result = validator.Validate(new OtpSendRequest("0912345678", "reset"));

        result.IsValid.Should().BeTrue();
    }
}
