using FluentValidation;
using VocaNova.API.Common.Constants;
namespace VocaNova.API.Features.Admin.Contracts.Requests;

public sealed class AdminUserQueryValidator : AbstractValidator<AdminUserQueryRequest>
{
    private static readonly IReadOnlySet<string> SortColumns =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id", "name", "email", "status", "phone" };

    public AdminUserQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(AppSettings.MaxPageLimit);
        RuleFor(query => query.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || UserStatus.All.Contains(status))
            .WithMessage("Status is invalid.");
        RuleFor(query => query.Search)
            .MaximumLength(100);
        RuleFor(query => query.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || SortColumns.Contains(sortBy))
            .WithMessage("Sort column is invalid.");
        RuleFor(query => query.SortDirection)
            .Must(direction => string.IsNullOrWhiteSpace(direction)
                || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
    }
}
