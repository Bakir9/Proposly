using Proposly.Application.Abstractions;

namespace Proposly.Application.Hr.Commands.SetWorkerNotePinned;

public sealed record SetWorkerNotePinnedCommand(Guid UserId, Guid NoteId, bool Pinned) : ICommand;
