using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.ApplyTaskTemplate;

public sealed class ApplyTaskTemplateCommandHandler(
    IProjectRepository projects,
    ITaskTemplateRepository templates) : ICommandHandler<ApplyTaskTemplateCommand, Guid>
{
    public async Task<Guid> HandleAsync(ApplyTaskTemplateCommand command, CancellationToken ct = default)
    {
        var project = await projects.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var template = await templates.GetByIdAsync(command.TemplateId, ct)
            ?? throw AppException.NotFound("task_template.not_found", $"Task template {command.TemplateId} not found.");

        // Parent carries the template's name and the summed estimate; steps carry their own.
        var parent = project.AddTask(
            template.Name,
            template.Description,
            template.TotalEstimatedHours(),
            command.StartDate,
            command.DueDate,
            command.MilestoneId,
            command.AssignedMemberId);

        foreach (var step in template.Steps.OrderBy(s => s.SortOrder))
        {
            project.AddTask(
                step.Title,
                null,
                step.EstimatedHours,
                command.StartDate,
                command.DueDate,
                command.MilestoneId,
                command.AssignedMemberId,
                parentTaskId: parent.Id);
        }

        await projects.UpdateAsync(project, ct);
        return parent.Id;
    }
}
