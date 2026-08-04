using FluentValidation;
using FluentValidation.Validators;
using VocaNova.API.Common.Constants;

namespace VocaNova.API.Common.Validation;

public sealed class CefrLevelValidator<T> : PropertyValidator<T, string?>
{
    public override string Name => "CefrLevelValidator";

    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        return value is null || CefrLevel.All.Contains(value);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "{PropertyName} must be null or one of: A1, A2, B1, B2, C1, C2.";
    }
}
