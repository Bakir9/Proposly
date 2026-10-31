using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Queries.GetDashboard;
using Proposly.Application.Dashboard.Queries.GetDashboardHome;
using Proposly.Application.Dashboard.Queries.GetWhosOut;
using Proposly.Application.Dashboard.Responses;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    [HttpGet]
    public async Task<DashboardResponse> Get(
        [FromServices] IQueryHandler<GetDashboardQuery, DashboardResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetDashboardQuery(), ct);

    [HttpGet("home")]
    public async Task<DashboardHomeResponse> GetHome(
        [FromServices] IQueryHandler<GetDashboardHomeQuery, DashboardHomeResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetDashboardHomeQuery(), ct);

    [HttpGet("whos-out")]
    public async Task<WhosOutResponse> GetWhosOut(
        [FromServices] IQueryHandler<GetWhosOutQuery, WhosOutResponse> handler,
        CancellationToken ct,
        [FromQuery] string range = "today")
        => await handler.HandleAsync(new GetWhosOutQuery(range), ct);
}
