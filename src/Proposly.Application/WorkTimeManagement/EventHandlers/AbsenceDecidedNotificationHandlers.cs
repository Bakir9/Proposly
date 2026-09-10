using Proposly.Application.Abstractions;
using Proposly.Domain.Notifications;
using Proposly.Domain.WorkTimeManagement.Events;

namespace Proposly.Application.WorkTimeManagement.EventHandlers;

public sealed class AbsenceApprovedNotificationHandler(
    INotificationRepository notifications) : IDomainEventHandler<AbsenceApprovedDomainEvent>
{
    public async Task HandleAsync(AbsenceApprovedDomainEvent e, CancellationToken ct = default)
    {
        // Self-approval needs no notification — approver and employee are the same person.
        if (e.ApproverId == e.UserId) return;

        var notification = Notification.Create(
            e.CompanyId,
            e.UserId,
            $"Your time off from {e.StartDate:dd MMM} to {e.EndDate:dd MMM} was approved",
            $"/absences?year={e.StartDate.Year}");

        await notifications.AddAsync(notification, ct);
        await notifications.SaveChangesAsync(ct);
    }
}

public sealed class AbsenceRejectedNotificationHandler(
    INotificationRepository notifications) : IDomainEventHandler<AbsenceRejectedDomainEvent>
{
    public async Task HandleAsync(AbsenceRejectedDomainEvent e, CancellationToken ct = default)
    {
        // The reason travels with the notification: being told no without knowing why is worse
        // than being told no.
        var notification = Notification.Create(
            e.CompanyId,
            e.UserId,
            $"Your time off from {e.StartDate:dd MMM} to {e.EndDate:dd MMM} was declined: {e.Reason}",
            $"/absences?year={e.StartDate.Year}");

        await notifications.AddAsync(notification, ct);
        await notifications.SaveChangesAsync(ct);
    }
}
