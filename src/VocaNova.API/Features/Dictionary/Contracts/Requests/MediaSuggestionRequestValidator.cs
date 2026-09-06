using FluentValidation;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class MediaSuggestionRequestValidator : AbstractValidator<MediaSuggestionRequest>
{
    public MediaSuggestionRequestValidator()
    {
        RuleFor(request => request.Type)
            .Must(MediaSuggestionTypes.IsSupported)
            .WithMessage("Media type must be image or video.");

        RuleFor(request => request.Limit)
            .InclusiveBetween(1, 20);

        RuleFor(request => request.Query)
            .MaximumLength(120);
    }
}
