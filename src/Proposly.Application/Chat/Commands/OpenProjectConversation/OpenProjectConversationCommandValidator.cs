using FluentValidation;

namespace Proposly.Application.Chat.Commands.OpenProjectConversation;

public sealed class OpenProjectConversationCommandValidator : AbstractValidator<OpenProjectConversationCommand>
{
    public OpenProjectConversationCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
    }
}
