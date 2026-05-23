using Proposly.Domain.CalendarManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.CalendarManagement.Events;

public sealed record InvitationRespondedDomainEvent(
    Guid TerminId,
    string TerminTitle,
    Guid InviteeId,
    string InviteeName,
    InvitationStatus NewStatus,
    Guid OrganizerId,
    Guid CompanyId) : IDomainEvent;
