using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Events;
using Proposly.Domain.Notifications;

namespace Proposly.Application.CalendarManagement.EventHandlers;

public sealed class TerminCancelledNotificationHandler(INotificationRepository repository)
    : IDomainEventHandler<TerminCancelledDomainEvent>
{
    public async Task HandleAsync(TerminCancelledDomainEvent e, CancellationToken ct = default)
    {
        foreach (var (userId, _) in e.Invitees)
        {
            var n = Notification.Create(e.CompanyId, userId,
                $"\"{e.Title}\" was cancelled",
                $"/calendar");
            await repository.AddAsync(n, ct);
        }
        await repository.SaveChangesAsync(ct);
    }
}
