using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTimeEntry;

public sealed class DeleteTimeEntryCommandHandler : ICommandHandler<DeleteTimeEntryCommand>
{
    private readonly IProjectRepository _repository;

    public DeleteTimeEntryCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(DeleteTimeEntryCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdWithTimeEntriesAsync(command.ProjectId, cancellationToken)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        project.RemoveTimeEntry(command.TimeEntryId);

        await _repository.UpdateAsync(project, cancellationToken);
    }
}
