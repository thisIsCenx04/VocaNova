using FluentValidation;
using FluentValidation.Validators;

namespace VocaNova.API.Common.Validation;

public sealed class StrongPasswordValidator<T> : PropertyValidator<T, string?>
{
    public override string Name => "StrongPasswordValidator";

    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        return value is { Length: >= 8 }
            && value.Any(char.IsUpper)
            && value.Any(char.IsLower)
            && value.Any(char.IsDigit);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "{PropertyName} must be at least 8 characters and contain uppercase, lowercase, and digit characters.";
    }
}
