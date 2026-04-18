using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.OfferManagement.Commands.AddOfferItem;

public sealed class AddOfferItemCommandHandler : ICommandHandler<AddOfferItemCommand, Guid>
{
    private readonly IOfferRepository _repository;

    public AddOfferItemCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task<Guid> HandleAsync(AddOfferItemCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        var item = offer.AddItem(command.Description, command.Quantity, new Money(command.UnitPrice, offer.Currency));

        await _repository.UpdateAsync(offer, ct);
        return item.Id;
    }
}
