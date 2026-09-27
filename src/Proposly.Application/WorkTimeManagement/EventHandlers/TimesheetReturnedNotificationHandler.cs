using Proposly.Application.Abstractions;
using Proposly.Domain.Notifications;
using Proposly.Domain.WorkTimeManagement.Events;

namespace Proposly.Application.WorkTimeManagement.EventHandlers;

public sealed class TimesheetReturnedNotificationHandler(
    INotificationRepository notifications)
    : IDomainEventHandler<TimesheetReturnedForCorrectionDomainEvent>
{
    public async Task HandleAsync(
        TimesheetReturnedForCorrectionDomainEvent e, CancellationToken ct = default)
    {
        var title = string.IsNullOrWhiteSpace(e.Reason)
            ? $"Your working time for {e.Year}-{e.Month:00} was returned for correction"
            : $"Your working time for {e.Year}-{e.Month:00} was returned: {e.Reason}";

        var notification = Notification.Create(
            e.CompanyId,
            e.UserId,
            title,
            $"/worktime?year={e.Year}&month={e.Month}");

        await notifications.AddAsync(notification, ct);
        await notifications.SaveChangesAsync(ct);
    }
}
