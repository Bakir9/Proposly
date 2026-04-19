using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Commands.AcceptOffer;
using Proposly.Application.OfferManagement.Commands.AddOfferItem;
using Proposly.Application.OfferManagement.Commands.ExpireOffer;
using Proposly.Application.OfferManagement.Commands.CreateOffer;
using Proposly.Application.OfferManagement.Commands.RejectOffer;
using Proposly.Application.OfferManagement.Commands.RemoveOfferItem;
using Proposly.Application.OfferManagement.Commands.SendOffer;
using Proposly.Application.OfferManagement.Commands.SendOfferEmail;
using Proposly.Application.OfferManagement.Commands.UpdateOffer;
using Proposly.Application.OfferManagement.Commands.UpdateOfferItem;
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

    [HttpPost("{id:guid}/expire")]
    public async Task<IActionResult> Expire(
        Guid id,
        [FromServices] ICommandHandler<ExpireOfferCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new ExpireOfferCommand(id), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOfferBody body,
        [FromServices] ICommandHandler<UpdateOfferCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateOfferCommand(id, body.Title, body.Notes, body.ValidUntil), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<Guid>> AddItem(
        Guid id,
        [FromBody] AddOfferItemBody body,
        [FromServices] ICommandHandler<AddOfferItemCommand, Guid> handler,
        CancellationToken ct)
    {
        var itemId = await handler.HandleAsync(new AddOfferItemCommand(id, body.Description, body.Quantity, body.UnitPrice), ct);
        return CreatedAtAction(nameof(GetById), new { id }, itemId);
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateOfferItemBody body,
        [FromServices] ICommandHandler<UpdateOfferItemCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateOfferItemCommand(id, itemId, body.Description, body.Quantity, body.UnitPrice), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(
        Guid id,
        Guid itemId,
        [FromServices] ICommandHandler<RemoveOfferItemCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RemoveOfferItemCommand(id, itemId), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IQueryHandler<GetOfferByIdQuery, OfferDetailResponse?> queryHandler,
        [FromServices] Proposly.Application.Abstractions.IPdfService pdfService,
        CancellationToken ct)
    {
        var offer = await queryHandler.HandleAsync(new GetOfferByIdQuery(id), ct);
        if (offer is null) return NotFound();

        byte[] bytes;
        try
        {
            bytes = pdfService.GenerateOfferPdf(offer);
        }
        catch (Exception ex)
        {
            return Problem($"PDF generation failed: {ex.Message}");
        }

        var fileName = $"Offer-{offer.Title.Replace(" ", "_")}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost("{id:guid}/email")]
    public async Task<IActionResult> SendEmail(
        Guid id,
        [FromServices] ICommandHandler<SendOfferEmailCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new SendOfferEmailCommand(id), ct);
        return NoContent();
    }
}

public record UpdateOfferBody(string Title, string? Notes, DateOnly? ValidUntil);
public record AddOfferItemBody(string Description, decimal Quantity, decimal UnitPrice);
public record UpdateOfferItemBody(string Description, decimal Quantity, decimal UnitPrice);
