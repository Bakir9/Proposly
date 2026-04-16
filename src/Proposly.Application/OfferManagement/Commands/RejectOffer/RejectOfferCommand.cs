using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.RejectOffer;

public record RejectOfferCommand(Guid OfferId) : ICommand;
