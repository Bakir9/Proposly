using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetEmploymentTerms;

/// <summary>
/// Version history for one employee, newest first. A member asking for a colleague gets an empty
/// list — the query filter hides the rows, so no role check is needed here.
/// </summary>
public sealed record GetEmploymentTermsQuery(Guid? UserId = null)
    : IQuery<IReadOnlyList<EmploymentTermsResponse>>;
