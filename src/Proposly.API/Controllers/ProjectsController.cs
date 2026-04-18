using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Commands.AddExpense;
using Proposly.Application.ProjectManagement.Commands.AddMilestone;
using Proposly.Application.ProjectManagement.Commands.AddProjectMember;
using Proposly.Application.ProjectManagement.Commands.AddTask;
using Proposly.Application.ProjectManagement.Commands.CompleteMilestone;
using Proposly.Application.ProjectManagement.Commands.CreateProject;
using Proposly.Application.ProjectManagement.Commands.LogTime;
using Proposly.Application.ProjectManagement.Commands.UpdateProject;
using Proposly.Application.ProjectManagement.Commands.UpdateTaskStatus;
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

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManageProjects)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProjectBody body,
        [FromServices] ICommandHandler<UpdateProjectCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateProjectCommand(id, body.Name, body.Description, body.Deadline, body.Status), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/members")]
    [Authorize(Policy = Policies.ManageProjects)]
    public async Task<ActionResult<Guid>> AddMember(
        Guid id,
        [FromBody] AddMemberBody body,
        [FromServices] ICommandHandler<AddProjectMemberCommand, Guid> handler,
        CancellationToken ct)
    {
        var memberId = await handler.HandleAsync(new AddProjectMemberCommand(id, body.UserId, body.Name, body.Role, body.HourlyRate, body.Currency), ct);
        return CreatedAtAction(nameof(GetById), new { id }, memberId);
    }

    [HttpPost("{id:guid}/time")]
    public async Task<ActionResult<Guid>> LogTime(
        Guid id,
        [FromBody] LogTimeBody body,
        [FromServices] ICommandHandler<LogTimeCommand, Guid> handler,
        CancellationToken ct)
    {
        var entryId = await handler.HandleAsync(new LogTimeCommand(id, body.MemberId, body.HoursWorked, body.Description, body.Date), ct);
        return CreatedAtAction(nameof(GetById), new { id }, entryId);
    }

    [HttpPost("{id:guid}/expenses")]
    [Authorize(Policy = Policies.ManageProjects)]
    public async Task<ActionResult<Guid>> AddExpense(
        Guid id,
        [FromBody] AddExpenseBody body,
        [FromServices] ICommandHandler<AddExpenseCommand, Guid> handler,
        CancellationToken ct)
    {
        var expenseId = await handler.HandleAsync(new AddExpenseCommand(id, body.Description, body.Amount, body.Currency, body.Category, body.Date), ct);
        return CreatedAtAction(nameof(GetById), new { id }, expenseId);
    }

    [HttpPost("{id:guid}/tasks")]
    [Authorize(Policy = Policies.ManageProjects)]
    public async Task<ActionResult<Guid>> AddTask(
        Guid id,
        [FromBody] AddTaskBody body,
        [FromServices] ICommandHandler<AddTaskCommand, Guid> handler,
        CancellationToken ct)
    {
        var taskId = await handler.HandleAsync(new AddTaskCommand(id, body.Title, body.Description, body.EstimatedHours, body.DueDate, body.MilestoneId), ct);
        return CreatedAtAction(nameof(GetById), new { id }, taskId);
    }

    [HttpPut("{id:guid}/tasks/{taskId:guid}/status")]
    public async Task<IActionResult> UpdateTaskStatus(
        Guid id,
        Guid taskId,
        [FromBody] UpdateTaskStatusBody body,
        [FromServices] ICommandHandler<UpdateTaskStatusCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateTaskStatusCommand(id, taskId, body.Status), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/milestones")]
    [Authorize(Policy = Policies.ManageProjects)]
    public async Task<ActionResult<Guid>> AddMilestone(
        Guid id,
        [FromBody] AddMilestoneBody body,
        [FromServices] ICommandHandler<AddMilestoneCommand, Guid> handler,
        CancellationToken ct)
    {
        var milestoneId = await handler.HandleAsync(new AddMilestoneCommand(id, body.Title, body.DueDate), ct);
        return CreatedAtAction(nameof(GetById), new { id }, milestoneId);
    }

    [HttpPut("{id:guid}/milestones/{milestoneId:guid}/complete")]
    [Authorize(Policy = Policies.ManageProjects)]
    public async Task<IActionResult> CompleteMilestone(
        Guid id,
        Guid milestoneId,
        [FromServices] ICommandHandler<CompleteMilestoneCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new CompleteMilestoneCommand(id, milestoneId), ct);
        return NoContent();
    }
}

public record UpdateProjectBody(string Name, string? Description, DateOnly? Deadline, string? Status);
public record AddMemberBody(Guid UserId, string Name, string Role, decimal HourlyRate, string Currency);
public record LogTimeBody(Guid MemberId, decimal HoursWorked, string? Description, DateOnly Date);
public record AddExpenseBody(string Description, decimal Amount, string Currency, string Category, DateOnly Date);
public record AddTaskBody(string Title, string? Description, decimal? EstimatedHours, DateOnly? DueDate, Guid? MilestoneId);
public record UpdateTaskStatusBody(string Status);
public record AddMilestoneBody(string Title, DateOnly DueDate);
