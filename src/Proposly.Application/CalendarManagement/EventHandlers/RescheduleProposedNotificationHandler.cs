using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Events;
using Proposly.Domain.Notifications;

namespace Proposly.Application.CalendarManagement.EventHandlers;

public sealed class RescheduleProposedNotificationHandler(INotificationRepository repository)
    : IDomainEventHandler<RescheduleProposedDomainEvent>
{
    public async Task HandleAsync(RescheduleProposedDomainEvent e, CancellationToken ct = default)
    {
        var dateStr = e.ProposedStart.ToLocalTime().ToString("ddd, MMM d 'at' h:mm tt");
        var n = Notification.Create(e.CompanyId, e.OrganizerId,
            $"{e.InviteeName} proposed a new time for \"{e.TerminTitle}\": {dateStr}",
            $"/calendar?termin={e.TerminId}");
        await repository.AddAsync(n, ct);
        await repository.SaveChangesAsync(ct);
    }
}
