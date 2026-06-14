using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Admin.DTOs;

namespace VocaNova.API.Features.Admin.Validators;

public sealed class AdminAuditLogQueryValidator : AbstractValidator<AdminAuditLogQuery>
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
