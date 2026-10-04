using FluentValidation;

namespace Proposly.Application.Chat.Commands.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Body).MaximumLength(4000);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Body) || x.AttachmentIds.Count > 0)
            .WithName(nameof(SendMessageCommand.Body))
            .WithMessage("A message needs text or at least one attachment.");

        RuleFor(x => x.AttachmentIds)
            .Must(ids => ids.Count <= 10)
            .WithMessage("A message can carry at most 10 attachments.");
    }
}
