using FluentValidation;
using VocaNova.API.Common.Constants;
namespace VocaNova.API.Features.Admin.Contracts.Requests;

public sealed class AdminAuditLogQueryValidator : AbstractValidator<AdminAuditLogQueryRequest>
{
    public AdminAuditLogQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(AppSettings.MaxPageLimit);
        RuleFor(query => query.Entity)
            .MaximumLength(50);
    }
}
