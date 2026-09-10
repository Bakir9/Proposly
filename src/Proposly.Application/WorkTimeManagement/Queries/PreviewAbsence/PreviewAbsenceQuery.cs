using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Queries.PreviewAbsence;

/// <summary>
/// What a prospective request would cost, so the form can show the balance before anything is
/// submitted. Read-only; the request handler recalculates rather than trusting this.
/// </summary>
public sealed record PreviewAbsenceQuery(
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool FirstDayIsHalf = false,
    bool LastDayIsHalf = false) : IQuery<AbsencePreviewResponse>;
