using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Commands.CreateClient;
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

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateClientCommand command,
        [FromServices] ICommandHandler<CreateClientCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }
}
