using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Enums;
using Proposly.Domain.CalendarManagement.Events;
using Proposly.Domain.Notifications;

namespace Proposly.Application.CalendarManagement.EventHandlers;

public sealed class InvitationRespondedNotificationHandler(INotificationRepository repository)
    : IDomainEventHandler<InvitationRespondedDomainEvent>
{
    public async Task HandleAsync(InvitationRespondedDomainEvent e, CancellationToken ct = default)
    {
        var verb = e.NewStatus == InvitationStatus.Accepted ? "accepted" : "declined";
        var n = Notification.Create(e.CompanyId, e.OrganizerId,
            $"{e.InviteeName} {verb} the invitation to \"{e.TerminTitle}\"",
            $"/calendar?termin={e.TerminId}");
        await repository.AddAsync(n, ct);
        await repository.SaveChangesAsync(ct);
    }
}
