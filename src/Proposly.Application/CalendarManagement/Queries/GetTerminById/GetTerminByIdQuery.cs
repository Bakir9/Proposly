using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;

namespace Proposly.Application.CalendarManagement.Queries.GetTerminById;

public record GetTerminByIdQuery(Guid Id) : IQuery<TerminDetailResponse?>;
