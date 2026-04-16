using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.SendOffer;

public record SendOfferCommand(Guid OfferId) : ICommand;
