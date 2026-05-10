using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Commands.AddClientNote;
using Proposly.Application.OfferManagement.Commands.CreateClient;
using Proposly.Application.OfferManagement.Commands.DeleteClient;
using Proposly.Application.OfferManagement.Commands.UpdateClient;
using Proposly.Application.OfferManagement.Queries.GetClientById;
using Proposly.Application.OfferManagement.Queries.GetClients;
using Proposly.Application.OfferManagement.Responses;

namespace Proposly.API.Controllers;

[Authorize(Policy = Policies.ManageClients)]
[ApiController]
[Route("api/[controller]")]
public sealed class ClientsController : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ClientSummaryResponse>> GetAll(
        [FromServices] IQueryHandler<GetClientsQuery, IReadOnlyList<ClientSummaryResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetClientsQuery(), ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDetailResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetClientByIdQuery, ClientDetailResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetClientByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateClientCommand command,
        [FromServices] ICommandHandler<CreateClientCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateClientBody body,
        [FromServices] ICommandHandler<UpdateClientCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateClientCommand(id, body.Name, body.ContactPerson, body.Email, body.Phone, body.Website, body.Street, body.City, body.PostalCode, body.Country, body.Currency, body.VatNumber, body.Status), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteClientCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteClientCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<Guid>> AddNote(
        Guid id,
        [FromBody] AddNoteBody body,
        [FromServices] ICommandHandler<AddClientNoteCommand, Guid> handler,
        CancellationToken ct)
    {
        var noteId = await handler.HandleAsync(new AddClientNoteCommand(id, body.Content), ct);
        return Ok(noteId);
    }
}

public record UpdateClientBody(string Name, string? ContactPerson, string? Email, string? Phone, string? Website, string? Street, string? City, string? PostalCode, string? Country, string? Currency, string? VatNumber, Proposly.Domain.OfferManagement.Enums.ClientStatus Status = Proposly.Domain.OfferManagement.Enums.ClientStatus.Active);
public record AddNoteBody(string Content);
