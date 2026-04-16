using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Commands.CreateProject;
using Proposly.Application.ProjectManagement.Queries.GetProjectById;
using Proposly.Application.ProjectManagement.Queries.GetProjects;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProjectsController : ControllerBase
{
    // All roles can view projects
    [HttpGet]
    public async Task<IReadOnlyList<ProjectSummaryResponse>> GetAll(
        [FromServices] IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectSummaryResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetProjectsQuery(), ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetProjectByIdQuery, ProjectDetailResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetProjectByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // Only Owner and Admin can create projects
    [HttpPost]
    [Authorize(Policy = Policies.ManageProjects)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateProjectCommand command,
        [FromServices] ICommandHandler<CreateProjectCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
}
