using Proposly.Shared.Primitives;

namespace Proposly.Domain.CalendarManagement.Events;

public sealed record TerminScheduledDomainEvent(
    Guid TerminId,
    string Title,
    DateTime Start,
    Guid OrganizerId,
    string OrganizerName,
    Guid CompanyId,
    IReadOnlyList<(Guid UserId, string Name)> Invitees) : IDomainEvent;
