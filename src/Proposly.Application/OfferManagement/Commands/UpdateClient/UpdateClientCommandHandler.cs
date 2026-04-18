using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.OfferManagement.Commands.UpdateClient;

public sealed class UpdateClientCommandHandler : ICommandHandler<UpdateClientCommand>
{
    private readonly IClientRepository _repository;

    public UpdateClientCommandHandler(IClientRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateClientCommand command, CancellationToken ct = default)
    {
        var client = await _repository.GetByIdAsync(command.ClientId, ct)
            ?? throw new InvalidOperationException($"Client {command.ClientId} not found.");

        Address? address = null;
        if (command.Street is not null || command.City is not null || command.PostalCode is not null || command.Country is not null)
            address = new Address(command.Street, command.City, command.PostalCode, command.Country);

        client.Update(command.Name, command.ContactPerson, command.Email, command.Phone, address);

        await _repository.UpdateAsync(client, ct);
    }
}
