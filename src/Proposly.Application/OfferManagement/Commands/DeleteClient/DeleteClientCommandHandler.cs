using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.OfferManagement.Commands.DeleteClient;

public sealed class DeleteClientCommandHandler : ICommandHandler<DeleteClientCommand>
{
    private readonly IClientRepository _repository;

    public DeleteClientCommandHandler(IClientRepository repository) => _repository = repository;

    public async Task HandleAsync(DeleteClientCommand command, CancellationToken ct = default)
    {
        var client = await _repository.GetByIdAsync(command.ClientId, ct)
            ?? throw AppException.NotFound("client.not_found", $"Client {command.ClientId} not found.");

        await _repository.DeleteAsync(client, ct);
    }
}
