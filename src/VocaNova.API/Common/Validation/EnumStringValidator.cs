using FluentValidation;
using FluentValidation.Validators;

namespace VocaNova.API.Common.Validation;

public sealed class EnumStringValidator<T> : PropertyValidator<T, string?>
{
    private readonly IReadOnlySet<string> _allowedValues;

    public EnumStringValidator(IEnumerable<string> allowedValues)
    {
        _allowedValues = allowedValues.ToHashSet(StringComparer.Ordinal);

        if (_allowedValues.Count == 0)
        {
            throw new ArgumentException("At least one allowed value is required.", nameof(allowedValues));
        }
    }

    public override string Name => "EnumStringValidator";

    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        return value is not null && _allowedValues.Contains(value);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "{PropertyName} must be one of the allowed values.";
    }
}
