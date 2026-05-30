using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTimeEntry;

public sealed class DeleteTimeEntryCommandHandler : ICommandHandler<DeleteTimeEntryCommand>
{
    private readonly IProjectRepository _repository;

    public DeleteTimeEntryCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(DeleteTimeEntryCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdWithTimeEntriesAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        project.RemoveTimeEntry(command.TimeEntryId);

        await _repository.UpdateAsync(project, cancellationToken);
    }
}
