using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Admin.DTOs;

namespace VocaNova.API.Features.Admin.Validators;

public sealed class AdminUserQueryValidator : AbstractValidator<AdminUserQuery>
{
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
    }
}
