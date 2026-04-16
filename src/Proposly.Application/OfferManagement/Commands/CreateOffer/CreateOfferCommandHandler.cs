using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.CreateOffer;

public sealed class CreateOfferCommandHandler : ICommandHandler<CreateOfferCommand, Guid>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateOfferCommandHandler(
        IOfferRepository offerRepository,
        IClientRepository clientRepository,
        ICurrentUserService currentUser)
    {
        _offerRepository = offerRepository;
        _clientRepository = clientRepository;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(CreateOfferCommand command, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(command.ClientId, ct)
            ?? throw new InvalidOperationException($"Client {command.ClientId} not found.");

        var offer = Offer.Create(
            _currentUser.CompanyId,
            client.Id,
            command.Title,
            command.Notes,
            command.Currency,
            command.ValidUntil);

        await _offerRepository.AddAsync(offer, ct);
        return offer.Id;
    }
}
