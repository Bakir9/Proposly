using Proposly.Application.Abstractions;

namespace Proposly.Application.OfferManagement.Commands.AddClientNote;

public record AddClientNoteCommand(Guid ClientId, string Content) : ICommand<Guid>;
