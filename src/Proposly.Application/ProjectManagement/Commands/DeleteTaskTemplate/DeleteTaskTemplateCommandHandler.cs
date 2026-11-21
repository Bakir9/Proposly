using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTaskTemplate;

public sealed class DeleteTaskTemplateCommandHandler(
    ITaskTemplateRepository templates) : ICommandHandler<DeleteTaskTemplateCommand>
{
    public async Task HandleAsync(DeleteTaskTemplateCommand command, CancellationToken ct = default)
    {
        var template = await templates.GetByIdAsync(command.TemplateId, ct)
            ?? throw AppException.NotFound("task_template.not_found", $"Task template {command.TemplateId} not found.");

        await templates.RemoveAsync(template, ct);
    }
}
