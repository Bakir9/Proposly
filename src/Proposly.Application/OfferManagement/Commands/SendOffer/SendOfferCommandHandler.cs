using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.OfferManagement.Commands.SendOffer;

public sealed class SendOfferCommandHandler : ICommandHandler<SendOfferCommand>
{
    private readonly IOfferRepository _repository;

    public SendOfferCommandHandler(IOfferRepository repository) => _repository = repository;

    public async Task HandleAsync(SendOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw AppException.NotFound("offer.not_found", $"Offer {command.OfferId} not found.");

        offer.Send();

        await _repository.UpdateAsync(offer, ct);
    }
}
