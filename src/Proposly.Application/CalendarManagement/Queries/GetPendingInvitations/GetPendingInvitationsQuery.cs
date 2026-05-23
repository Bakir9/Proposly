using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;

namespace Proposly.Application.CalendarManagement.Queries.GetPendingInvitations;

public sealed record GetPendingInvitationsQuery : IQuery<IReadOnlyList<TerminSummaryResponse>>;
