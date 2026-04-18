using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.OfferManagement.Commands.UpdateOfferItem;

public sealed class UpdateOfferItemCommandHandler : ICommandHandler<UpdateOfferItemCommand>
{
    private readonly IOfferRepository _repository;

    public UpdateOfferItemCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateOfferItemCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        offer.UpdateItem(command.ItemId, command.Description, command.Quantity, new Money(command.UnitPrice, offer.Currency));

        await _repository.UpdateAsync(offer, ct);
    }
}
