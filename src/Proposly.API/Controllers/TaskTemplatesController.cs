using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Commands.DeleteTaskTemplate;
using Proposly.Application.ProjectManagement.Commands.SaveTaskTemplate;
using Proposly.Application.ProjectManagement.Queries.GetTaskTemplates;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ManageProjects)]
public sealed class TaskTemplatesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskTemplateResponse>>> GetAll(
        [FromServices] IQueryHandler<GetTaskTemplatesQuery, IReadOnlyList<TaskTemplateResponse>> handler,
        CancellationToken ct)
    {
        return Ok(await handler.HandleAsync(new GetTaskTemplatesQuery(), ct));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] SaveTaskTemplateCommand command,
        [FromServices] ICommandHandler<SaveTaskTemplateCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteTaskTemplateCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteTaskTemplateCommand(id), ct);
        return NoContent();
    }
}
