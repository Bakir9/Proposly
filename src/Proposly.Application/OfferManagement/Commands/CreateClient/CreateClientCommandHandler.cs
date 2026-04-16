using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.OfferManagement.Commands.CreateClient;

public sealed class CreateClientCommandHandler : ICommandHandler<CreateClientCommand, Guid>
{
    private readonly IClientRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CreateClientCommandHandler(IClientRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(CreateClientCommand command, CancellationToken ct = default)
    {
        Address? address = null;
        if (command.Street is not null || command.City is not null || command.PostalCode is not null || command.Country is not null)
            address = new Address(command.Street, command.City, command.PostalCode, command.Country);

        var client = Client.Create(
            _currentUser.CompanyId,
            command.Name,
            command.ContactPerson,
            command.Email,
            command.Phone,
            address);

        await _repository.AddAsync(client, ct);
        return client.Id;
    }
}
