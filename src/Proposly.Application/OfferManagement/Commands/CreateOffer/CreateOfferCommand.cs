using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.CreateOffer;

public record CreateOfferCommand(
    Guid ClientId,
    string Title,
    string? Notes,
    string Currency,
    DateOnly? ValidUntil,
    decimal? DiscountPercent = null,
    decimal? VatRateOverride = null) : ICommand<Guid>;
