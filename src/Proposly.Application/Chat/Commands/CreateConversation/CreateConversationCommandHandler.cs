using Proposly.Application.Abstractions;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Chat.Commands.CreateConversation;

public sealed class CreateConversationCommandHandler(
    IConversationRepository conversations,
    IUserRepository users,
    ICurrentUserService currentUser) : ICommandHandler<CreateConversationCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateConversationCommand command, CancellationToken ct = default)
    {
        var participantIds = command.ParticipantUserIds.Distinct().Where(id => id != currentUser.UserId).ToList();
        if (participantIds.Count == 0)
            throw new InvalidOperationException("A conversation needs at least one other participant.");

        // Every participant must be a colleague in the same company; the tenant filter on Users
        // makes a foreign id simply not resolve.
        foreach (var id in participantIds)
        {
            _ = await users.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException($"User {id} not found.");
        }

        if (command.Kind == "Direct")
        {
            var other = participantIds[0];
            var existing = await conversations.GetDirectBetweenAsync(currentUser.UserId, other, ct);
            if (existing is not null)
                return existing.Id;

            var direct = Conversation.CreateDirect(currentUser.CompanyId, currentUser.UserId, other);
            await conversations.AddAsync(direct, ct);
            await conversations.SaveChangesAsync(ct);
            return direct.Id;
        }

        var allParticipants = participantIds.Append(currentUser.UserId).ToList();
        var group = Conversation.CreateGroup(currentUser.CompanyId, command.Title!, allParticipants);
        await conversations.AddAsync(group, ct);
        await conversations.SaveChangesAsync(ct);
        return group.Id;
    }
}
