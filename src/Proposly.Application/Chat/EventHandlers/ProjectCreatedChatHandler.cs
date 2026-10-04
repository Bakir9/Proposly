using Proposly.Application.Abstractions;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.ProjectManagement.Events;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.Chat.EventHandlers;

/// <summary>Every project gets one channel; it is created together with the project.</summary>
public sealed class ProjectCreatedChatHandler(
    IConversationRepository conversations,
    IProjectRepository projects) : IDomainEventHandler<ProjectCreatedDomainEvent>
{
    public async Task HandleAsync(ProjectCreatedDomainEvent e, CancellationToken ct = default)
    {
        if (await conversations.GetByProjectIdAsync(e.ProjectId, ct) is not null)
            return;

        var project = await projects.GetByIdAsync(e.ProjectId, ct);
        if (project is null)
            return;

        var channel = Conversation.CreateProjectChannel(e.CompanyId, project.Id, project.Name);
        channel.EnsureParticipants(project.Members.Select(m => m.UserId).ToList());

        await conversations.AddAsync(channel, ct);
        await conversations.SaveChangesAsync(ct);
    }
}
