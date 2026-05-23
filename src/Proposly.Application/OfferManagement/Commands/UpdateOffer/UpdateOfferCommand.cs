using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.UpdateOffer;

public record UpdateOfferCommand(Guid OfferId, string Title, string? Notes, DateOnly? ValidUntil, decimal? DiscountPercent = null, decimal? VatRateOverride = null) : ICommand;
