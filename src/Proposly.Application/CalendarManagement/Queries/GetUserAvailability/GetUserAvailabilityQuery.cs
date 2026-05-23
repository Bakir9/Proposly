using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;

namespace Proposly.Application.CalendarManagement.Queries.GetUserAvailability;

public record GetUserAvailabilityQuery(Guid UserId, DateTime Start, DateTime End) : IQuery<UserAvailabilityResponse>;
