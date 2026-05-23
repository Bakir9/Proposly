using Proposly.Shared.Primitives;

namespace Proposly.Domain.CalendarManagement.Events;

public sealed record TerminRescheduledDomainEvent(
    Guid TerminId,
    string Title,
    DateTime NewStart,
    DateTime NewEnd,
    Guid OrganizerId,
    Guid CompanyId,
    IReadOnlyList<(Guid UserId, string Name)> Invitees) : IDomainEvent;
