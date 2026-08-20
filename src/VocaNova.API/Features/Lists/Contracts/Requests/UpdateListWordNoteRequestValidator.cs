using FluentValidation;
using VocaNova.API.Features.Lists.Contracts.Requests;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed class UpdateListWordNoteRequestValidator : AbstractValidator<UpdateListWordNoteRequest>
{
    public UpdateListWordNoteRequestValidator()
    {
        RuleFor(request => request.Note)
            .MaximumLength(1000);
    }
}
