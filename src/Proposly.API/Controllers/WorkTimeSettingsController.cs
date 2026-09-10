using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.CreateWorkTimePolicy;
using Proposly.Application.WorkTimeManagement.Queries.GetPolicyDefaults;
using Proposly.Application.WorkTimeManagement.Queries.GetWorkTimePolicy;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.API.Controllers;

/// <summary>
/// Company-level working time configuration. Employment terms and the holiday calendar join this
/// controller in a later phase.
/// </summary>
[Authorize(Policy = Policies.ManageWorkTimeSettings)]
[ApiController]
[Route("api/worktime-settings")]
public sealed class WorkTimeSettingsController : ControllerBase
{
    /// <summary>
    /// The policy in force, with its version history. 404 means none is configured, which the
    /// client must present as a prompt rather than as "no limits apply".
    /// </summary>
    [HttpGet("policy")]
    [Authorize(Policy = Policies.RecordOwnWorkTime)]
    public async Task<ActionResult<WorkTimePolicyResponse>> GetPolicy(
        [FromServices] IQueryHandler<GetWorkTimePolicyQuery, WorkTimePolicyResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetWorkTimePolicyQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Unsaved seed values for a jurisdiction, for the policy form.</summary>
    [HttpGet("policy/defaults")]
    public async Task<ActionResult<WorkTimePolicyResponse>> GetPolicyDefaults(
        [FromQuery] string jurisdiction,
        [FromServices] IQueryHandler<GetPolicyDefaultsQuery, WorkTimePolicyResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetPolicyDefaultsQuery(jurisdiction), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("policy")]
    public async Task<ActionResult<Guid>> CreatePolicy(
        [FromBody] CreateWorkTimePolicyCommand command,
        [FromServices] ICommandHandler<CreateWorkTimePolicyCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetPolicy), new { }, id);
    }
}
