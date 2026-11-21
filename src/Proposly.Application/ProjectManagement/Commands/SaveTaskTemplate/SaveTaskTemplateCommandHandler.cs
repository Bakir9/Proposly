using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.SaveTaskTemplate;

public sealed class SaveTaskTemplateCommandHandler(
    ITaskTemplateRepository templates,
    ICurrentUserService currentUser) : ICommandHandler<SaveTaskTemplateCommand, Guid>
{
    public async Task<Guid> HandleAsync(SaveTaskTemplateCommand command, CancellationToken ct = default)
    {
        var template = TaskTemplate.Create(currentUser.CompanyId, command.Name.Trim(), command.Description);
        foreach (var step in command.Steps)
            template.AddStep(step.Title.Trim(), step.EstimatedHours);

        await templates.AddAsync(template, ct);
        return template.Id;
    }
}
