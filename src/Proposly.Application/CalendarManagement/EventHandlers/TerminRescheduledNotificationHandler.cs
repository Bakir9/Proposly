using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Events;
using Proposly.Domain.Notifications;

namespace Proposly.Application.CalendarManagement.EventHandlers;

public sealed class TerminRescheduledNotificationHandler(INotificationRepository repository)
    : IDomainEventHandler<TerminRescheduledDomainEvent>
{
    public async Task HandleAsync(TerminRescheduledDomainEvent e, CancellationToken ct = default)
    {
        var dateStr = e.NewStart.ToLocalTime().ToString("ddd, MMM d 'at' h:mm tt");
        foreach (var (userId, _) in e.Invitees)
        {
            var n = Notification.Create(e.CompanyId, userId,
                $"\"{e.Title}\" was rescheduled to {dateStr}",
                $"/calendar?termin={e.TerminId}");
            await repository.AddAsync(n, ct);
        }
        await repository.SaveChangesAsync(ct);
    }
}
