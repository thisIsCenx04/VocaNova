using FluentValidation;
using FluentValidation.Validators;

namespace VocaNova.API.Common.Validation;

public sealed class DateRangeValidator<T> : PropertyValidator<T, T>
{
    private const int MaxRangeDays = 365;

    private readonly Func<T, DateOnly?> _fromSelector;
    private readonly Func<T, DateOnly?> _toSelector;

    public DateRangeValidator(Func<T, DateOnly?> fromSelector, Func<T, DateOnly?> toSelector)
    {
        _fromSelector = fromSelector;
        _toSelector = toSelector;
    }

    public override string Name => "DateRangeValidator";

    public override bool IsValid(ValidationContext<T> context, T value)
    {
        var from = _fromSelector(value);
        var to = _toSelector(value);

        if (from is null || to is null)
        {
            return true;
        }

        if (from > to)
        {
            return false;
        }

        return to.Value.DayNumber - from.Value.DayNumber <= MaxRangeDays;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "Date range must have from less than or equal to to and must not exceed 365 days.";
    }
}
