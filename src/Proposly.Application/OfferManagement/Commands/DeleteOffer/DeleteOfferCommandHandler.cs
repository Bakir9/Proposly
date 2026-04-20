using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.DeleteOffer;

public sealed class DeleteOfferCommandHandler : ICommandHandler<DeleteOfferCommand>
{
    private readonly IOfferRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public DeleteOfferCommandHandler(IOfferRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(DeleteOfferCommand command, CancellationToken ct = default)
    {
        var offer = await _repository.GetByIdAsync(command.OfferId, ct)
            ?? throw new InvalidOperationException($"Offer {command.OfferId} not found.");

        var isAdmin = _currentUser.Role == "Admin";

        if (!isAdmin)
        {
            var isDeletable = offer.Status is OfferStatus.Draft or OfferStatus.Expired;
            if (!isDeletable)
                throw new InvalidOperationException(
                    "Only Draft or Expired offers can be deleted. Admins can delete any offer.");
        }

        await _repository.DeleteAsync(offer, ct);
    }
}
