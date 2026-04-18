using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.UpdateOfferItem;

public record UpdateOfferItemCommand(Guid OfferId, Guid ItemId, string Description, decimal Quantity, decimal UnitPrice) : ICommand;
