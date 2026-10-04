using FluentValidation;

namespace Proposly.Application.Chat.Commands.MarkConversationUnread;

public sealed class MarkConversationUnreadCommandValidator : AbstractValidator<MarkConversationUnreadCommand>
{
    public MarkConversationUnreadCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
