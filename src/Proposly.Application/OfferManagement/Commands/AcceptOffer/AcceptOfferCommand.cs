using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.AcceptOffer;

public record AcceptOfferCommand(Guid OfferId) : ICommand;
