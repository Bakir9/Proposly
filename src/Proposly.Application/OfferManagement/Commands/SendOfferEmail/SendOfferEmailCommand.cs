using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.SendOfferEmail;

public record SendOfferEmailCommand(Guid OfferId) : ICommand;
