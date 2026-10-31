using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProjectMember;

public sealed class UpdateProjectMemberCommandHandler : ICommandHandler<UpdateProjectMemberCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateProjectMemberCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateProjectMemberCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        project.UpdateMemberEngagement(
            command.MemberId,
            command.Role,
            new Money(command.CostRate, project.Budget.Currency),
            new Money(command.BillRate, project.Budget.Currency),
            command.AllocatedHours);

        await _repository.UpdateAsync(project, cancellationToken);
    }
}
