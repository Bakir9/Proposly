using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Queries.GetDashboard;
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
}
