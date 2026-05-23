using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Settings.Commands.UpdateCompanySettings;
using Proposly.Application.Settings.Queries.GetCompanySettings;
using Proposly.Application.Settings.Responses;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class SettingsController : ControllerBase
{
    [HttpGet("company")]
    public async Task<ActionResult<CompanySettingsResponse>> GetCompanySettings(
        [FromServices] IQueryHandler<GetCompanySettingsQuery, CompanySettingsResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetCompanySettingsQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("company")]
    public async Task<IActionResult> UpdateCompanySettings(
        [FromBody] UpdateCompanySettingsCommand command,
        [FromServices] ICommandHandler<UpdateCompanySettingsCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(command, ct);
        return NoContent();
    }
}
