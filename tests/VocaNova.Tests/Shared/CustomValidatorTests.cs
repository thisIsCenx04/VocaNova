using FluentAssertions;
using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Validation;

namespace VocaNova.Tests.Shared;

public class CustomValidatorTests
{
    [Theory]
    [InlineData("0912345678", true)]
    [InlineData("0212345678", false)]
    [InlineData("0312345678", true)]
    public void VietnamesePhoneValidator_Should_Validate_Phone_Format(string? phone, bool expectedIsValid)
    {
        var validator = new PhoneRequestValidator();

        var result = validator.Validate(new PhoneRequest(phone));

        result.IsValid.Should().Be(expectedIsValid);
    }

    [Theory]
    [InlineData("Password1", true)]
    [InlineData("password1", false)]
    [InlineData("Aa123456", true)]
    public void StrongPasswordValidator_Should_Validate_Password_Complexity(string? password, bool expectedIsValid)
    {
        var validator = new PasswordRequestValidator();

        var result = validator.Validate(new PasswordRequest(password));

        result.IsValid.Should().Be(expectedIsValid);
    }

    [Theory]
    [InlineData("A1", true)]
    [InlineData("D1", false)]
    [InlineData(null, true)]
    public void CefrLevelValidator_Should_Allow_Null_Or_Valid_Cefr_Level(string? cefrLevel, bool expectedIsValid)
    {
        var validator = new CefrRequestValidator();

        var result = validator.Validate(new CefrRequest(cefrLevel));

        result.IsValid.Should().Be(expectedIsValid);
    }

    [Theory]
    [InlineData("standard", true)]
    [InlineData("arcade", false)]
    [InlineData("elimination", true)]
    public void EnumStringValidator_Should_Validate_Allowed_String_Set(string? mode, bool expectedIsValid)
    {
        var validator = new ModeRequestValidator();

        var result = validator.Validate(new ModeRequest(mode));

        result.IsValid.Should().Be(expectedIsValid);
    }

    [Theory]
    [MemberData(nameof(DateRangeCases))]
    public void DateRangeValidator_Should_Validate_Order_And_Max_Range(
        DateOnly? from,
        DateOnly? to,
        bool expectedIsValid)
    {
        var validator = new DateRangeRequestValidator();

        var result = validator.Validate(new DateRangeRequest(from, to));

        result.IsValid.Should().Be(expectedIsValid);
    }

    public static TheoryData<DateOnly?, DateOnly?, bool> DateRangeCases()
    {
        return new TheoryData<DateOnly?, DateOnly?, bool>
        {
            { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), true },
            { new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 31), false },
            { new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), true },
            { new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 2), false },
        };
    }

    private sealed record PhoneRequest(string? Phone);

    private sealed class PhoneRequestValidator : AbstractValidator<PhoneRequest>
    {
        public PhoneRequestValidator()
        {
            RuleFor(request => request.Phone).VietnamesePhone();
        }
    }

    private sealed record PasswordRequest(string? Password);

    private sealed class PasswordRequestValidator : AbstractValidator<PasswordRequest>
    {
        public PasswordRequestValidator()
        {
            RuleFor(request => request.Password).StrongPassword();
        }
    }

    private sealed record CefrRequest(string? CefrLevel);

    private sealed class CefrRequestValidator : AbstractValidator<CefrRequest>
    {
        public CefrRequestValidator()
        {
            RuleFor(request => request.CefrLevel).CefrLevel();
        }
    }

    private sealed record ModeRequest(string? Mode);

    private sealed class ModeRequestValidator : AbstractValidator<ModeRequest>
    {
        public ModeRequestValidator()
        {
            RuleFor(request => request.Mode).EnumString(TestMode.All);
        }
    }

    private sealed record DateRangeRequest(DateOnly? From, DateOnly? To);

    private sealed class DateRangeRequestValidator : AbstractValidator<DateRangeRequest>
    {
        public DateRangeRequestValidator()
        {
            RuleFor(request => request)
                .DateRange(request => request.From, request => request.To);
        }
    }
}
