using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetTargetHours;

/// <summary>
/// Expected working days and contracted hours for a month, with approved absence netted off.
/// </summary>
public sealed record GetTargetHoursQuery(int Year, int Month, Guid? UserId = null)
    : IQuery<TargetHoursResponse>;
