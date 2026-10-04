using FluentValidation;

namespace Proposly.Application.Chat.Commands.MarkConversationRead;

public sealed class MarkConversationReadCommandValidator : AbstractValidator<MarkConversationReadCommand>
{
    public MarkConversationReadCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
