using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Responses;

namespace Proposly.Application.Dashboard.Queries.GetDashboardHome;

/// <summary>The redesigned home dashboard: attention cards, KPIs, offers, projects, my time.</summary>
public sealed record GetDashboardHomeQuery : IQuery<DashboardHomeResponse>;
