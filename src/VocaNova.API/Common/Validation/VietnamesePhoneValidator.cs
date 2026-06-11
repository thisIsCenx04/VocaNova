using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Validators;

namespace VocaNova.API.Common.Validation;

public sealed partial class VietnamesePhoneValidator<T> : PropertyValidator<T, string?>
{
    public override string Name => "VietnamesePhoneValidator";

    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && VietnamesePhoneRegex().IsMatch(value);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "{PropertyName} must be a valid Vietnamese phone number.";
    }

    [GeneratedRegex("^(0[3-9]\\d{8})$")]
    private static partial Regex VietnamesePhoneRegex();
}
