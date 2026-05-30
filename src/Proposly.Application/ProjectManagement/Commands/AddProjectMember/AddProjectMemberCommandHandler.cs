using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.AddProjectMember;

public sealed class AddProjectMemberCommandHandler : ICommandHandler<AddProjectMemberCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public AddProjectMemberCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Guid> HandleAsync(AddProjectMemberCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var member = project.AddMember(command.UserId, command.Name, command.Role, new Money(command.HourlyRate, command.Currency));
        await _repository.UpdateAsync(project, cancellationToken);
        return member.Id;
    }
}
