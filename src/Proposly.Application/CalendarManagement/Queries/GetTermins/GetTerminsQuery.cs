using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;

namespace Proposly.Application.CalendarManagement.Queries.GetTermins;

public record GetTerminsQuery(DateTime Start, DateTime End) : IQuery<IReadOnlyList<TerminSummaryResponse>>;
