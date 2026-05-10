using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.ExtendOffer;

public record ExtendOfferCommand(Guid OfferId, DateOnly NewValidUntil) : ICommand;
