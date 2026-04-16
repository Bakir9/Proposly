using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.AcceptOffer;

public sealed class AcceptOfferCommandHandler : ICommandHandler<AcceptOfferCommand>
{
    private readonly IOfferRepository _repository;

    public AcceptOfferCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(AcceptOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        offer.Accept();

        await _repository.UpdateAsync(offer, ct);
    }
}
