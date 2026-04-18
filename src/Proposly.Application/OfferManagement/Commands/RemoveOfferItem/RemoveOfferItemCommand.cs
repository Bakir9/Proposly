using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.RemoveOfferItem;

public record RemoveOfferItemCommand(Guid OfferId, Guid ItemId) : ICommand;
