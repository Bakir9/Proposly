using Proposly.Application.Abstractions;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.ProjectManagement.Events;

namespace Proposly.Application.Chat.EventHandlers;

/// <summary>People assigned to a project join its channel automatically.</summary>
public sealed class ProjectMemberAddedChatHandler(
    IConversationRepository conversations) : IDomainEventHandler<ProjectMemberAddedDomainEvent>
{
    public async Task HandleAsync(ProjectMemberAddedDomainEvent e, CancellationToken ct = default)
    {
        var channel = await conversations.GetByProjectIdAsync(e.ProjectId, ct);
        if (channel is null)
        {
            // Projects that predate the chat feature have no channel yet.
            channel = Conversation.CreateProjectChannel(e.CompanyId, e.ProjectId, e.ProjectName);
            await conversations.AddAsync(channel, ct);
        }

        channel.EnsureParticipants([e.UserId]);
        await conversations.SaveChangesAsync(ct);
    }
}
