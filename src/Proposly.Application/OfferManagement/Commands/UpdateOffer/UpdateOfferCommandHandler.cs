using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.UpdateOffer;

public sealed class UpdateOfferCommandHandler : ICommandHandler<UpdateOfferCommand>
{
    private readonly IOfferRepository _repository;

    public UpdateOfferCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        offer.UpdateDetails(command.Title, command.Notes, command.ValidUntil);

        await _repository.UpdateAsync(offer, ct);
    }
}
