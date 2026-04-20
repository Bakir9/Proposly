using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.DeleteOffer;

public record DeleteOfferCommand(Guid OfferId) : ICommand;
