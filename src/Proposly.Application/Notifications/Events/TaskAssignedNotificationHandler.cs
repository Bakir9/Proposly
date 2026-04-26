using Proposly.Application.Abstractions;
using Proposly.Domain.Notifications;
using Proposly.Domain.ProjectManagement.Events;

namespace Proposly.Application.Notifications.Events;

public sealed class TaskAssignedNotificationHandler(INotificationRepository repository)
    : IDomainEventHandler<TaskAssignedDomainEvent>
{
    public async Task HandleAsync(TaskAssignedDomainEvent e, CancellationToken ct = default)
    {
        var notification = Notification.Create(
            e.CompanyId,
            e.AssignedUserId,
            $"You've been assigned to \"{e.TaskTitle}\" in {e.ProjectName}",
            $"/projects/{e.ProjectId}?task={e.TaskId}");

        await repository.AddAsync(notification, ct);
        await repository.SaveChangesAsync(ct);
    }
}
