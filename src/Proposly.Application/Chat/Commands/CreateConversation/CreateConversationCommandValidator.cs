using FluentValidation;

namespace Proposly.Application.Chat.Commands.CreateConversation;

public sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.Kind)
            .Must(k => k is "Direct" or "Group")
            .WithMessage("Kind must be 'Direct' or 'Group'.");

        RuleFor(x => x.ParticipantUserIds).NotEmpty();
        RuleForEach(x => x.ParticipantUserIds).NotEmpty();

        RuleFor(x => x.ParticipantUserIds)
            .Must(ids => ids.Count == 1)
            .When(x => x.Kind == "Direct")
            .WithMessage("A direct conversation takes exactly one other participant.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Kind == "Group")
            .WithMessage("A group conversation needs a title.");

        RuleFor(x => x.Title)
            .Empty()
            .When(x => x.Kind == "Direct")
            .WithMessage("A direct conversation has no title.");
    }
}
