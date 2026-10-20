using FluentValidation;

namespace Proposly.Application.Hr.Commands.SetWorkerNotePinned;

public sealed class SetWorkerNotePinnedCommandValidator : AbstractValidator<SetWorkerNotePinnedCommand>
{
    public SetWorkerNotePinnedCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NoteId).NotEmpty();
    }
}
