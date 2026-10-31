using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.SetBillingType;

public sealed class SetBillingTypeCommandHandler : ICommandHandler<SetBillingTypeCommand>
{
    private readonly IProjectRepository _repository;

    public SetBillingTypeCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(SetBillingTypeCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var billingType = Enum.Parse<BillingType>(command.BillingType);
        project.SetBillingType(billingType);

        await _repository.UpdateAsync(project, cancellationToken);
    }
}
