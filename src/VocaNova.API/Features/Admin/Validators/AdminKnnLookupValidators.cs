using FluentValidation;
using System.Linq.Expressions;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Admin.DTOs;

namespace VocaNova.API.Features.Admin.Validators;

public sealed class KnnLookupQueryValidator : AbstractValidator<KnnLookupQuery>
{
    private static readonly string[] SortFields =
    [
        "id", "name", "status", "min_age", "max_age", "display_order",
        "code", "parent", "description",
    ];

    public KnnLookupQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(AppSettings.MaxPageLimit);
        RuleFor(query => query.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || UserStatus.All.Contains(status))
            .WithMessage("Status is invalid.");
        RuleFor(query => query.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || SortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Sort field is invalid.");
        RuleFor(query => query.SortDirection)
            .Must(direction => string.IsNullOrWhiteSpace(direction)
                || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction is invalid.");
    }
}

public sealed class CreateAgeRangeRequestValidator : AbstractValidator<CreateAgeRangeRequest>
{
    public CreateAgeRangeRequestValidator()
    {
        Include(new AgeRangeRules<CreateAgeRangeRequest>(
            request => request.Name,
            request => request.MinAge,
            request => request.MaxAge,
            request => request.DisplayOrder));
    }
}

public sealed class UpdateAgeRangeRequestValidator : AbstractValidator<UpdateAgeRangeRequest>
{
    public UpdateAgeRangeRequestValidator()
    {
        Include(new AgeRangeRules<UpdateAgeRangeRequest>(
            request => request.Name,
            request => request.MinAge,
            request => request.MaxAge,
            request => request.DisplayOrder));
    }
}

public sealed class CreateRegionRequestValidator : AbstractValidator<CreateRegionRequest>
{
    public CreateRegionRequestValidator()
    {
        Include(new RegionRules<CreateRegionRequest>(
            request => request.Name,
            request => request.Code));
    }
}

public sealed class UpdateRegionRequestValidator : AbstractValidator<UpdateRegionRequest>
{
    public UpdateRegionRequestValidator()
    {
        Include(new RegionRules<UpdateRegionRequest>(
            request => request.Name,
            request => request.Code));
    }
}

public sealed class CreateOccupationRequestValidator : AbstractValidator<CreateOccupationRequest>
{
    public CreateOccupationRequestValidator()
    {
        Include(new NamedDescriptionRules<CreateOccupationRequest>(
            request => request.Name,
            request => request.Description));
    }
}

public sealed class UpdateOccupationRequestValidator : AbstractValidator<UpdateOccupationRequest>
{
    public UpdateOccupationRequestValidator()
    {
        Include(new NamedDescriptionRules<UpdateOccupationRequest>(
            request => request.Name,
            request => request.Description));
    }
}

public sealed class CreateEducationLevelRequestValidator : AbstractValidator<CreateEducationLevelRequest>
{
    public CreateEducationLevelRequestValidator()
    {
        Include(new EducationLevelRules<CreateEducationLevelRequest>(
            request => request.Name,
            request => request.Description,
            request => request.DisplayOrder));
    }
}

public sealed class UpdateEducationLevelRequestValidator : AbstractValidator<UpdateEducationLevelRequest>
{
    public UpdateEducationLevelRequestValidator()
    {
        Include(new EducationLevelRules<UpdateEducationLevelRequest>(
            request => request.Name,
            request => request.Description,
            request => request.DisplayOrder));
    }
}

public sealed class CreateLearningPurposeRequestValidator : AbstractValidator<CreateLearningPurposeRequest>
{
    public CreateLearningPurposeRequestValidator()
    {
        Include(new NamedDescriptionRules<CreateLearningPurposeRequest>(
            request => request.Name,
            request => request.Description));
    }
}

public sealed class UpdateLearningPurposeRequestValidator : AbstractValidator<UpdateLearningPurposeRequest>
{
    public UpdateLearningPurposeRequestValidator()
    {
        Include(new NamedDescriptionRules<UpdateLearningPurposeRequest>(
            request => request.Name,
            request => request.Description));
    }
}

internal sealed class AgeRangeRules<T> : AbstractValidator<T>
{
    public AgeRangeRules(
        Expression<Func<T, string?>> name,
        Expression<Func<T, int?>> minAge,
        Expression<Func<T, int?>> maxAge,
        Expression<Func<T, int>> displayOrder)
    {
        var minAgeFunc = minAge.Compile();
        var maxAgeFunc = maxAge.Compile();

        RuleFor(name)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(minAge)
            .GreaterThanOrEqualTo(0)
            .When(request => minAgeFunc(request).HasValue);
        RuleFor(maxAge)
            .GreaterThanOrEqualTo(0)
            .When(request => maxAgeFunc(request).HasValue);
        RuleFor(request => request)
            .Must(request => !minAgeFunc(request).HasValue
                || !maxAgeFunc(request).HasValue
                || minAgeFunc(request) <= maxAgeFunc(request))
            .WithMessage("MinAge must be less than or equal to MaxAge.");
        RuleFor(displayOrder).GreaterThanOrEqualTo(0);
    }
}

internal sealed class RegionRules<T> : AbstractValidator<T>
{
    public RegionRules(Expression<Func<T, string?>> name, Expression<Func<T, string?>> code)
    {
        RuleFor(name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(code)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Code may contain only letters, numbers, underscore, and hyphen.");
    }
}

internal sealed class NamedDescriptionRules<T> : AbstractValidator<T>
{
    public NamedDescriptionRules(Expression<Func<T, string?>> name, Expression<Func<T, string?>> description)
    {
        RuleFor(name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(description)
            .MaximumLength(255);
    }
}

internal sealed class EducationLevelRules<T> : AbstractValidator<T>
{
    public EducationLevelRules(
        Expression<Func<T, string?>> name,
        Expression<Func<T, string?>> description,
        Expression<Func<T, int>> displayOrder)
    {
        RuleFor(name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(description)
            .MaximumLength(255);
        RuleFor(displayOrder).GreaterThanOrEqualTo(0);
    }
}
