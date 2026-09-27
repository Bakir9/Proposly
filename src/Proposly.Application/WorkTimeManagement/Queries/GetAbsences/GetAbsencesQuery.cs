using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Queries.GetAbsences;

/// <summary>
/// Absences for a year. Returns the caller's own unless they may view all employees — the query
/// filter decides, so the same endpoint serves both.
/// </summary>
public sealed record GetAbsencesQuery(int Year, AbsenceStatus? Status = null)
    : IQuery<IReadOnlyList<AbsenceResponse>>;
