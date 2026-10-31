using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.OfferManagement.Commands.RemoveOfferItem;

public sealed class RemoveOfferItemCommandHandler : ICommandHandler<RemoveOfferItemCommand>
{
    private readonly IOfferRepository _repository;

    public RemoveOfferItemCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(RemoveOfferItemCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw AppException.NotFound("offer.not_found", $"Offer {command.OfferId} not found.");

        offer.RemoveItem(command.ItemId);

        await _repository.UpdateAsync(offer, ct);
    }
}
