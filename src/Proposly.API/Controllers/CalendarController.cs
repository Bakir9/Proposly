using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Commands.CancelTermin;
using Proposly.Application.CalendarManagement.Commands.CreateTermin;
using Proposly.Application.CalendarManagement.Commands.DeleteTermin;
using Proposly.Application.CalendarManagement.Commands.ProposeReschedule;
using Proposly.Application.CalendarManagement.Commands.RescheduleTermin;
using Proposly.Application.CalendarManagement.Commands.RespondToInvitation;
using Proposly.Application.CalendarManagement.Commands.RespondToProposal;
using Proposly.Application.CalendarManagement.Commands.UpdateTermin;
using Proposly.Application.CalendarManagement.Queries.GetPendingInvitations;
using Proposly.Application.CalendarManagement.Queries.GetTerminById;
using Proposly.Application.CalendarManagement.Queries.GetTermins;
using Proposly.Application.CalendarManagement.Queries.GetUserAvailability;
using Proposly.Application.CalendarManagement.Responses;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class CalendarController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TerminSummaryResponse>>> GetTermins(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromServices] IQueryHandler<GetTerminsQuery, IReadOnlyList<TerminSummaryResponse>> handler,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(new GetTerminsQuery(start, end), ct));

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<TerminSummaryResponse>>> GetPendingInvitations(
        [FromServices] IQueryHandler<GetPendingInvitationsQuery, IReadOnlyList<TerminSummaryResponse>> handler,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(new GetPendingInvitationsQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TerminDetailResponse>> GetTerminById(
        Guid id,
        [FromServices] IQueryHandler<GetTerminByIdQuery, TerminDetailResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTerminByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("availability/{userId:guid}")]
    public async Task<ActionResult<UserAvailabilityResponse>> GetAvailability(
        Guid userId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromServices] IQueryHandler<GetUserAvailabilityQuery, UserAvailabilityResponse> handler,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(new GetUserAvailabilityQuery(userId, start, end), ct));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateTerminCommand command,
        [FromServices] ICommandHandler<CreateTerminCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetTerminById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTerminRequest body,
        [FromServices] ICommandHandler<UpdateTerminCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateTerminCommand(id, body.Title, body.Description, body.Location), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(
        Guid id,
        [FromBody] RescheduleTerminRequest body,
        [FromServices] ICommandHandler<RescheduleTerminCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RescheduleTerminCommand(id, body.NewStart, body.NewEnd), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] ICommandHandler<CancelTerminCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new CancelTerminCommand(id), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteTerminCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteTerminCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> RespondToInvitation(
        Guid id,
        [FromBody] RespondToInvitationRequest body,
        [FromServices] ICommandHandler<RespondToInvitationCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RespondToInvitationCommand(id, body.Action), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/propose-reschedule")]
    public async Task<IActionResult> ProposeReschedule(
        Guid id,
        [FromBody] ProposeRescheduleRequest body,
        [FromServices] ICommandHandler<ProposeRescheduleCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ProposeRescheduleCommand(id, body.ProposedStart, body.ProposedEnd, body.Message), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/respond-to-proposal")]
    public async Task<IActionResult> RespondToProposal(
        Guid id,
        [FromBody] RespondToProposalRequest body,
        [FromServices] ICommandHandler<RespondToProposalCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RespondToProposalCommand(id, body.InviteeId, body.Accept), ct);
        return NoContent();
    }
}

public record UpdateTerminRequest(string Title, string? Description, string? Location);
public record RescheduleTerminRequest(DateTime NewStart, DateTime NewEnd);
public record RespondToInvitationRequest(string Action);
public record ProposeRescheduleRequest(DateTime ProposedStart, DateTime ProposedEnd, string? Message);
public record RespondToProposalRequest(Guid InviteeId, bool Accept);
