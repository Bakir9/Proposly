using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.ExpireOffer;

public record ExpireOfferCommand(Guid OfferId) : ICommand;
