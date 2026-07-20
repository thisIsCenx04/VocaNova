using FluentValidation;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Validation;
using VocaNova.API.Features.SuperAdmin.DTOs;

namespace VocaNova.API.Features.SuperAdmin.Validators;

public sealed class AdminAccountQueryValidator : AbstractValidator<AdminAccountQuery>
{
    public AdminAccountQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(AppSettings.MaxPageLimit);
        RuleFor(query => query.Status)
            .Must(BeEmptyOrValidStatus)
            .WithMessage("Status must be active, locked, or deleted.");
        RuleFor(query => query.Search).MaximumLength(100);
    }

    private static bool BeEmptyOrValidStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) || UserStatus.All.Contains(status.Trim().ToLowerInvariant());
}

public sealed class CreateAdminAccountRequestValidator : AbstractValidator<CreateAdminAccountRequest>
{
    public CreateAdminAccountRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MinimumLength(2).MaximumLength(150);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(request => request.Phone).VietnamesePhone();
        RuleFor(request => request.Password).StrongPassword();
        RuleFor(request => request.Status)
            .Must(BeEmptyOrManageableStatus)
            .WithMessage("Status must be active or locked.");
    }

    private static bool BeEmptyOrManageableStatus(string? status) =>
        string.IsNullOrWhiteSpace(status)
        || status.Trim().ToLowerInvariant() is UserStatus.Active or UserStatus.Locked;
}

public sealed class UpdateAdminAccountRequestValidator : AbstractValidator<UpdateAdminAccountRequest>
{
    public UpdateAdminAccountRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MinimumLength(2).MaximumLength(150);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(request => request.Phone).VietnamesePhone();
        RuleFor(request => request.Password)
            .StrongPassword()
            .When(request => !string.IsNullOrWhiteSpace(request.Password));
        RuleFor(request => request.Status)
            .Must(BeEmptyOrManageableStatus)
            .WithMessage("Status must be active or locked.");
    }

    private static bool BeEmptyOrManageableStatus(string? status) =>
        string.IsNullOrWhiteSpace(status)
        || status.Trim().ToLowerInvariant() is UserStatus.Active or UserStatus.Locked;
}
