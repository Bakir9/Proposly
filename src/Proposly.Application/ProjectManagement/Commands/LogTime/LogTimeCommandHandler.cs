using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.LogTime;

public sealed class LogTimeCommandHandler : ICommandHandler<LogTimeCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public LogTimeCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Guid> HandleAsync(LogTimeCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var entry = project.LogTime(command.MemberId, command.HoursWorked, command.Description, command.Date);
        await _repository.UpdateAsync(project, cancellationToken);
        return entry.Id;
    }
}
