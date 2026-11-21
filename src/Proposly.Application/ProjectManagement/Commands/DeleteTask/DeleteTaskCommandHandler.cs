using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler(
    IProjectRepository projects) : ICommandHandler<DeleteTaskCommand>
{
    public async Task HandleAsync(DeleteTaskCommand command, CancellationToken ct = default)
    {
        var project = await projects.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        project.RemoveTask(command.TaskId);

        await projects.UpdateAsync(project, ct);
    }
}
