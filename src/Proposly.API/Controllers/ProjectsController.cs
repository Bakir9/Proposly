using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Commands.AddExpense;
using Proposly.Application.ProjectManagement.Commands.AddMilestone;
using Proposly.Application.ProjectManagement.Commands.AddProjectMember;
using Proposly.Application.ProjectManagement.Commands.AddTask;
using Proposly.Application.ProjectManagement.Commands.AddTaskComment;
using Proposly.Application.ProjectManagement.Commands.AddProjectNote;
using Proposly.Application.ProjectManagement.Commands.DeleteProjectNote;
using Proposly.Application.ProjectManagement.Commands.UpdateProjectNote;
using Proposly.Application.ProjectManagement.Commands.AddTaskDependency;
using Proposly.Application.ProjectManagement.Commands.RemoveTaskDependency;
using Proposly.Application.ProjectManagement.Commands.CompleteMilestone;
using Proposly.Application.ProjectManagement.Commands.CreateProject;
using Proposly.Application.ProjectManagement.Commands.DeleteTaskComment;
using Proposly.Application.ProjectManagement.Commands.EditTaskComment;
using Proposly.Application.ProjectManagement.Commands.LogTime;
using Proposly.Application.ProjectManagement.Commands.UpdateProject;
using Proposly.Application.ProjectManagement.Commands.UpdateTask;
using Proposly.Application.ProjectManagement.Commands.UpdateTaskStatus;
using Proposly.Application.ProjectManagement.Queries.GetBurndown;
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
        await handler.HandleAsync(new UpdateProjectCommand(id, body.Name, body.Description, body.Deadline, body.Status, body.BudgetAmount, body.BudgetCurrency), ct);
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
    public async Task<ActionResult<Guid>> AddTask(
        Guid id,
        [FromBody] AddTaskBody body,
        [FromServices] ICommandHandler<AddTaskCommand, Guid> handler,
        CancellationToken ct)
    {
        var taskId = await handler.HandleAsync(new AddTaskCommand(id, body.Title, body.Description, body.EstimatedHours, body.StartDate, body.DueDate, body.MilestoneId, body.AssignedMemberId), ct);
        return CreatedAtAction(nameof(GetById), new { id }, taskId);
    }

    [HttpPut("{id:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(
        Guid id,
        Guid taskId,
        [FromBody] UpdateTaskBody body,
        [FromServices] ICommandHandler<UpdateTaskCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateTaskCommand(id, taskId, body.Title, body.Description, body.EstimatedHours, body.StartDate, body.DueDate, body.MilestoneId, body.AssignedMemberId), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/tasks/{taskId:guid}/status")]
    public async Task<IActionResult> UpdateTaskStatus(
        Guid id,
        Guid taskId,
        [FromBody] UpdateTaskStatusBody body,
        [FromServices] ICommandHandler<UpdateTaskStatusCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateTaskStatusCommand(id, taskId, body.Status, body.ActualHours), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<Guid>> AddNote(
        Guid id,
        [FromBody] NoteBody body,
        [FromServices] ICommandHandler<AddProjectNoteCommand, Guid> handler,
        CancellationToken ct)
    {
        var noteId = await handler.HandleAsync(new AddProjectNoteCommand(id, body.Title, body.Content), ct);
        return CreatedAtAction(nameof(GetById), new { id }, noteId);
    }

    [HttpPut("{id:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> UpdateNote(
        Guid id,
        Guid noteId,
        [FromBody] NoteBody body,
        [FromServices] ICommandHandler<UpdateProjectNoteCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new UpdateProjectNoteCommand(id, noteId, body.Title, body.Content), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteNote(
        Guid id,
        Guid noteId,
        [FromServices] ICommandHandler<DeleteProjectNoteCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteProjectNoteCommand(id, noteId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/tasks/{taskId:guid}/dependencies")]
    public async Task<IActionResult> AddTaskDependency(
        Guid id,
        Guid taskId,
        [FromBody] AddTaskDependencyBody body,
        [FromServices] ICommandHandler<AddTaskDependencyCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new AddTaskDependencyCommand(id, taskId, body.BlockingTaskId), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/tasks/{taskId:guid}/dependencies/{blockingTaskId:guid}")]
    public async Task<IActionResult> RemoveTaskDependency(
        Guid id,
        Guid taskId,
        Guid blockingTaskId,
        [FromServices] ICommandHandler<RemoveTaskDependencyCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new RemoveTaskDependencyCommand(id, taskId, blockingTaskId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/tasks/{taskId:guid}/comments")]
    public async Task<ActionResult<Guid>> AddTaskComment(
        Guid id,
        Guid taskId,
        [FromBody] AddTaskCommentBody body,
        [FromServices] ICommandHandler<AddTaskCommentCommand, Guid> handler,
        CancellationToken ct)
    {
        var commentId = await handler.HandleAsync(new AddTaskCommentCommand(id, taskId, body.Body), ct);
        return CreatedAtAction(nameof(GetById), new { id }, commentId);
    }

    [HttpPut("{id:guid}/tasks/{taskId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> EditTaskComment(
        Guid id,
        Guid taskId,
        Guid commentId,
        [FromBody] EditTaskCommentBody body,
        [FromServices] ICommandHandler<EditTaskCommentCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new EditTaskCommentCommand(id, taskId, commentId, body.Body), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/tasks/{taskId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteTaskComment(
        Guid id,
        Guid taskId,
        Guid commentId,
        [FromServices] ICommandHandler<DeleteTaskCommentCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteTaskCommentCommand(id, taskId, commentId), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/burndown")]
    public async Task<ActionResult<BurndownResponse>> GetBurndown(
        Guid id,
        [FromServices] IQueryHandler<GetBurndownQuery, BurndownResponse?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetBurndownQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
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

public record UpdateProjectBody(string Name, string? Description, DateOnly? Deadline, string? Status, decimal? BudgetAmount, string? BudgetCurrency);
public record AddMemberBody(Guid UserId, string Name, string Role, decimal HourlyRate, string Currency);
public record LogTimeBody(Guid MemberId, decimal HoursWorked, string? Description, DateOnly Date);
public record AddExpenseBody(string Description, decimal Amount, string Currency, string Category, DateOnly Date);
public record AddTaskBody(string Title, string? Description, decimal? EstimatedHours, DateOnly? StartDate, DateOnly? DueDate, Guid? MilestoneId, Guid? AssignedMemberId);
public record UpdateTaskBody(string Title, string? Description, decimal? EstimatedHours, DateOnly? StartDate, DateOnly? DueDate, Guid? MilestoneId, Guid? AssignedMemberId);
public record UpdateTaskStatusBody(string Status, decimal? ActualHours);
public record AddMilestoneBody(string Title, DateOnly DueDate);
public record NoteBody(string Title, string Content);
public record AddTaskDependencyBody(Guid BlockingTaskId);
public record AddTaskCommentBody(string Body);
public record EditTaskCommentBody(string Body);
