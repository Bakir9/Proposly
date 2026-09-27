using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetPendingAbsences;

public sealed record GetPendingAbsencesQuery : IQuery<IReadOnlyList<AbsenceResponse>>;
