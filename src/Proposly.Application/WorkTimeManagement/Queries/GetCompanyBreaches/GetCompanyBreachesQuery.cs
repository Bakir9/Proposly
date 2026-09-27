using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetCompanyBreaches;

/// <summary>
/// Every outstanding working time breach across the company for one month.
/// <para>
/// Scoped to a month rather than the arbitrary date range in the contract, so it matches the other
/// company view and stays a single indexed lookup.
/// </para>
/// </summary>
public sealed record GetCompanyBreachesQuery(int Year, int Month)
    : IQuery<IReadOnlyList<BreachOverviewResponse>>;
