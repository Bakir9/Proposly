using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.OfferManagement.Commands.ExtendOffer;

public sealed class ExtendOfferCommandHandler : ICommandHandler<ExtendOfferCommand>
{
    private readonly IOfferRepository _repository;

    public ExtendOfferCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(ExtendOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw AppException.NotFound("offer.not_found", $"Offer {command.OfferId} not found.");

        offer.Extend(command.NewValidUntil);
        await _repository.UpdateAsync(offer, ct);
    }
}
