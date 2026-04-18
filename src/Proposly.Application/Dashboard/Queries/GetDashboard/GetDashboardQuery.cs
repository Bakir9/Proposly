using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Responses;

namespace Proposly.Application.Dashboard.Queries.GetDashboard;

public record GetDashboardQuery : IQuery<DashboardResponse>;
