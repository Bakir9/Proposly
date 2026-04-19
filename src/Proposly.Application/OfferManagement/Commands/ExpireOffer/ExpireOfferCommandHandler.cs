using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.ExpireOffer;

public sealed class ExpireOfferCommandHandler : ICommandHandler<ExpireOfferCommand>
{
    private readonly IOfferRepository _repository;

    public ExpireOfferCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(ExpireOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        offer.Expire();

        await _repository.UpdateAsync(offer, ct);
    }
}
