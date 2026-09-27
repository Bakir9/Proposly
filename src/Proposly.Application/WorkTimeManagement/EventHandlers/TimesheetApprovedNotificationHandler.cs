using Proposly.Application.Abstractions;
using Proposly.Domain.Notifications;
using Proposly.Domain.WorkTimeManagement.Events;

namespace Proposly.Application.WorkTimeManagement.EventHandlers;

public sealed class TimesheetApprovedNotificationHandler(
    INotificationRepository notifications) : IDomainEventHandler<TimesheetApprovedDomainEvent>
{
    public async Task HandleAsync(TimesheetApprovedDomainEvent e, CancellationToken ct = default)
    {
        // Self-approval needs no notification — the approver and the employee are the same person.
        if (e.ApproverId == e.UserId) return;

        var notification = Notification.Create(
            e.CompanyId,
            e.UserId,
            $"Your working time for {e.Year}-{e.Month:00} was approved",
            $"/worktime?year={e.Year}&month={e.Month}");

        await notifications.AddAsync(notification, ct);
        await notifications.SaveChangesAsync(ct);
    }
}
