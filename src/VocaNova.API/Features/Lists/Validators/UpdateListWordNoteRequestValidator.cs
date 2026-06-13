using FluentValidation;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Validators;

public sealed class UpdateListWordNoteRequestValidator : AbstractValidator<UpdateListWordNoteRequest>
{
    public UpdateListWordNoteRequestValidator()
    {
        RuleFor(request => request.Note)
            .MaximumLength(1000);
    }
}
