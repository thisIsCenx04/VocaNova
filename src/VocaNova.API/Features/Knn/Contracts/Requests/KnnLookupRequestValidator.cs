using FluentValidation;
using VocaNova.API.Common.Constants;

namespace VocaNova.API.Features.Knn.Contracts.Requests;

public sealed class KnnLookupRequestValidator : AbstractValidator<KnnLookupRequest>
{
    private static readonly string[] SortFields =
    [
        "id", "name", "status", "min_age", "max_age", "display_order",
        "code", "parent", "description",
    ];

    public KnnLookupRequestValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(AppSettings.MaxPageLimit);
        RuleFor(query => query.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || UserStatus.All.Contains(status))
            .WithMessage("Status is invalid.");
        RuleFor(query => query.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy)
                || SortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Sort field is invalid.");
        RuleFor(query => query.SortDirection)
            .Must(direction => string.IsNullOrWhiteSpace(direction)
                || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction is invalid.");
    }
}
