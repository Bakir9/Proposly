using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetCompanyMonthOverview;

/// <summary>Every employee's month-end figures in one call, for the owner and admin view.</summary>
public sealed record GetCompanyMonthOverviewQuery(int Year, int Month)
    : IQuery<IReadOnlyList<CompanyMonthOverviewResponse>>;
