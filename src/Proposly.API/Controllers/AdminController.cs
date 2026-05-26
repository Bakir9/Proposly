using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.Admin.Commands.AdminUpdateCompanyPlan;
using Proposly.Application.Admin.Queries.GetAllCompanies;

namespace Proposly.API.Controllers;

[Authorize(Policy = Policies.SuperAdminOnly)]
[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    [HttpGet("companies")]
    public async Task<ActionResult<List<CompanyAdminSummary>>> GetAllCompanies(
        [FromServices] IQueryHandler<GetAllCompaniesQuery, List<CompanyAdminSummary>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetAllCompaniesQuery(), ct);
        return Ok(result);
    }

    [HttpPut("companies/{companyId:guid}/plan")]
    public async Task<IActionResult> UpdateCompanyPlan(
        Guid companyId,
        [FromBody] AdminUpdateCompanyPlanCommand command,
        [FromServices] ICommandHandler<AdminUpdateCompanyPlanCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(command with { CompanyId = companyId }, ct);
        return NoContent();
    }
}
