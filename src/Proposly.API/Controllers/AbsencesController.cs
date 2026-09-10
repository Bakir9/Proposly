using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.ApproveAbsence;
using Proposly.Application.WorkTimeManagement.Commands.CancelAbsence;
using Proposly.Application.WorkTimeManagement.Commands.RejectAbsence;
using Proposly.Application.WorkTimeManagement.Commands.RequestAbsence;
using Proposly.Application.WorkTimeManagement.Commands.SetEntitlement;
using Proposly.Application.WorkTimeManagement.Queries.GetAbsences;
using Proposly.Application.WorkTimeManagement.Queries.GetEntitlement;
using Proposly.Application.WorkTimeManagement.Queries.GetPendingAbsences;
using Proposly.Application.WorkTimeManagement.Queries.PreviewAbsence;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.API.Controllers;

/// <summary>
/// Absence requests and vacation entitlement.
/// <para>
/// A member and an approver call the same list endpoints: the query filter returns own rows to one
/// and the whole company to the other, so there is no role branch here. Sick leave carries its
/// type and dates only — no response exposes health detail, because none is stored.
/// </para>
/// </summary>
[Authorize(Policy = Policies.RecordOwnWorkTime)]
[ApiController]
[Route("api/[controller]")]
public sealed class AbsencesController : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<AbsenceResponse>> GetAll(
        [FromQuery] int year,
        [FromQuery] AbsenceStatus? status,
        [FromServices] IQueryHandler<GetAbsencesQuery, IReadOnlyList<AbsenceResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetAbsencesQuery(year, status), ct);

    [HttpGet("entitlement")]
    public async Task<ActionResult<AbsenceEntitlementResponse>> GetEntitlement(
        [FromQuery] int year,
        [FromQuery] Guid? userId,
        [FromServices] IQueryHandler<GetEntitlementQuery, AbsenceEntitlementResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetEntitlementQuery(year, userId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>What a prospective request would cost, for the request form.</summary>
    [HttpGet("preview")]
    public async Task<AbsencePreviewResponse> Preview(
        [FromQuery] AbsenceType type,
        [FromQuery] DateOnly start,
        [FromQuery] DateOnly end,
        [FromQuery] bool firstHalf,
        [FromQuery] bool lastHalf,
        [FromServices] IQueryHandler<PreviewAbsenceQuery, AbsencePreviewResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(
            new PreviewAbsenceQuery(type, start, end, firstHalf, lastHalf), ct);

    [HttpPost]
    public async Task<ActionResult<Guid>> Request(
        [FromBody] RequestAbsenceCommand command,
        [FromServices] ICommandHandler<RequestAbsenceCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { year = command.StartDate.Year }, id);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] ICommandHandler<CancelAbsenceCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new CancelAbsenceCommand(id), ct);
        return NoContent();
    }

    // --- Approval ---

    [HttpGet("pending")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IReadOnlyList<AbsenceResponse>> GetPending(
        [FromServices] IQueryHandler<GetPendingAbsencesQuery, IReadOnlyList<AbsenceResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetPendingAbsencesQuery(), ct);

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromServices] ICommandHandler<ApproveAbsenceCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ApproveAbsenceCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Policies.ApproveWorkTime)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectAbsenceRequest body,
        [FromServices] ICommandHandler<RejectAbsenceCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RejectAbsenceCommand(id, body.Reason), ct);
        return NoContent();
    }

    // --- Entitlement administration ---

    [HttpPut("entitlement/{userId:guid}/{year:int}")]
    [Authorize(Policy = Policies.ManageWorkTimeSettings)]
    public async Task<IActionResult> SetEntitlement(
        Guid userId,
        int year,
        [FromBody] SetEntitlementRequest body,
        [FromServices] ICommandHandler<SetEntitlementCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(
            new SetEntitlementCommand(userId, year, body.EntitledDays, body.CarriedOverDays), ct);

        return NoContent();
    }
}

public sealed record RejectAbsenceRequest(string Reason);

public sealed record SetEntitlementRequest(decimal EntitledDays, decimal CarriedOverDays = 0m);
