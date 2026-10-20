using Proposly.Application.Abstractions;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Application.Hr.Commands.SetWorkerDocumentCategory;

public sealed class SetWorkerDocumentCategoryCommandHandler(
    IEmployeeProfileRepository profiles) : ICommandHandler<SetWorkerDocumentCategoryCommand>
{
    public async Task HandleAsync(SetWorkerDocumentCategoryCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Worker {command.UserId} has no HR profile.");

        profile.RecategorizeDocument(command.DocumentId, command.Category);
        await profiles.SaveChangesAsync(ct);
    }
}
