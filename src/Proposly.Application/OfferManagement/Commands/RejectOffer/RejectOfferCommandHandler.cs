using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.RejectOffer;

public sealed class RejectOfferCommandHandler : ICommandHandler<RejectOfferCommand>
{
    private readonly IOfferRepository _repository;

    public RejectOfferCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(RejectOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        offer.Reject();

        await _repository.UpdateAsync(offer, ct);
    }
}
