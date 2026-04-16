using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Commands.AcceptOffer;
using Proposly.Application.OfferManagement.Commands.CreateOffer;
using Proposly.Application.OfferManagement.Commands.RejectOffer;
using Proposly.Application.OfferManagement.Commands.SendOffer;
using Proposly.Application.OfferManagement.Queries.GetOfferById;
using Proposly.Application.OfferManagement.Queries.GetOffers;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.API.Controllers;

[Authorize(Policy = Policies.ManageOffers)]
[ApiController]
[Route("api/[controller]")]
public sealed class OffersController : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<OfferSummaryResponse>> GetAll(
        [FromQuery] OfferStatus? status,
        [FromServices] IQueryHandler<GetOffersQuery, IReadOnlyList<OfferSummaryResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetOffersQuery(status), ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OfferDetailResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetOfferByIdQuery, OfferDetailResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetOfferByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateOfferCommand command,
        [FromServices] ICommandHandler<CreateOfferCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(
        Guid id,
        [FromServices] ICommandHandler<SendOfferCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new SendOfferCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromServices] ICommandHandler<AcceptOfferCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new AcceptOfferCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromServices] ICommandHandler<RejectOfferCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RejectOfferCommand(id), ct);
        return NoContent();
    }
}
