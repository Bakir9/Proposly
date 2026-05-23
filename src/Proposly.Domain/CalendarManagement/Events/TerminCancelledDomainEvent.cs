using Proposly.Shared.Primitives;

namespace Proposly.Domain.CalendarManagement.Events;

public sealed record TerminCancelledDomainEvent(
    Guid TerminId,
    string Title,
    Guid OrganizerId,
    Guid CompanyId,
    IReadOnlyList<(Guid UserId, string Name)> Invitees) : IDomainEvent;
