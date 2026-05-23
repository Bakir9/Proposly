using Proposly.Shared.Primitives;

namespace Proposly.Domain.CalendarManagement.Events;

public sealed record RescheduleProposedDomainEvent(
    Guid TerminId,
    string TerminTitle,
    Guid InviteeId,
    string InviteeName,
    DateTime ProposedStart,
    DateTime ProposedEnd,
    Guid OrganizerId,
    Guid CompanyId) : IDomainEvent;
