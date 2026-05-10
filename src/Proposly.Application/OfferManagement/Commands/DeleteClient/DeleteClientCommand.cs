using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.DeleteClient;

public record DeleteClientCommand(Guid ClientId) : ICommand;
