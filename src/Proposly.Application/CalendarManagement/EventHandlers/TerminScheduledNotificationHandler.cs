using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Events;
using Proposly.Domain.Notifications;

namespace Proposly.Application.CalendarManagement.EventHandlers;

public sealed class TerminScheduledNotificationHandler(INotificationRepository repository)
    : IDomainEventHandler<TerminScheduledDomainEvent>
{
    public async Task HandleAsync(TerminScheduledDomainEvent e, CancellationToken ct = default)
    {
        var dateStr = e.Start.ToLocalTime().ToString("ddd, MMM d 'at' h:mm tt");
        foreach (var (userId, _) in e.Invitees)
        {
            var n = Notification.Create(e.CompanyId, userId,
                $"{e.OrganizerName} invited you to \"{e.Title}\" — {dateStr}",
                $"/calendar?termin={e.TerminId}");
            await repository.AddAsync(n, ct);
        }
        await repository.SaveChangesAsync(ct);
    }
}
