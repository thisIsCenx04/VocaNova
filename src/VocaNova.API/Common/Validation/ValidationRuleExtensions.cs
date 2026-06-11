using FluentValidation;

namespace VocaNova.API.Common.Validation;

public static class ValidationRuleExtensions
{
    public static IRuleBuilderOptions<T, string?> VietnamesePhone<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new VietnamesePhoneValidator<T>());
    }

    public static IRuleBuilderOptions<T, string?> StrongPassword<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new StrongPasswordValidator<T>());
    }

    public static IRuleBuilderOptions<T, string?> CefrLevel<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new CefrLevelValidator<T>());
    }

    public static IRuleBuilderOptions<T, string?> EnumString<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        IEnumerable<string> allowedValues)
    {
        return ruleBuilder.SetValidator(new EnumStringValidator<T>(allowedValues));
    }

    public static IRuleBuilderOptions<T, T> DateRange<T>(
        this IRuleBuilder<T, T> ruleBuilder,
        Func<T, DateOnly?> fromSelector,
        Func<T, DateOnly?> toSelector)
    {
        return ruleBuilder.SetValidator(new DateRangeValidator<T>(fromSelector, toSelector));
    }
}
