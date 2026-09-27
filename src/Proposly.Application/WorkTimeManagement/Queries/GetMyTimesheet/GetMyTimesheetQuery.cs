using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetMyTimesheet;

public sealed record GetMyTimesheetQuery(int Year, int Month) : IQuery<TimesheetDetailResponse?>;
