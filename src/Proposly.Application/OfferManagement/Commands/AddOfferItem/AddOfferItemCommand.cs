using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.AddOfferItem;

public record AddOfferItemCommand(Guid OfferId, string Description, decimal Quantity, decimal UnitPrice) : ICommand<Guid>;
