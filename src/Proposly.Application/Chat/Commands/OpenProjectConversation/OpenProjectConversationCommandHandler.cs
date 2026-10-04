using Proposly.Application.Abstractions;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.Chat.Commands.OpenProjectConversation;

public sealed class OpenProjectConversationCommandHandler(
    IConversationRepository conversations,
    IProjectRepository projects,
    ICurrentUserService currentUser) : ICommandHandler<OpenProjectConversationCommand, Guid>
{
    public async Task<Guid> HandleAsync(OpenProjectConversationCommand command, CancellationToken ct = default)
    {
        var project = await projects.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var memberUserIds = project.Members.Select(m => m.UserId).Distinct().ToList();

        // Assigned members join automatically; Owner/Admin may oversee any project channel.
        // Everyone else has no business in the discussion.
        if (!memberUserIds.Contains(currentUser.UserId) && !currentUser.CanViewAllEmployees)
            throw new InvalidOperationException("Only project members can open this discussion.");

        var participantIds = memberUserIds.Contains(currentUser.UserId)
            ? memberUserIds
            : memberUserIds.Append(currentUser.UserId).ToList();

        var conversation = await conversations.GetByProjectIdAsync(project.Id, ct);
        if (conversation is null)
        {
            conversation = Conversation.CreateProjectChannel(currentUser.CompanyId, project.Id, project.Name);
            conversation.EnsureParticipants(participantIds);
            await conversations.AddAsync(conversation, ct);
        }
        else
        {
            conversation.EnsureParticipants(participantIds);
        }

        await conversations.SaveChangesAsync(ct);
        return conversation.Id;
    }
}
