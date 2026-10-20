using Proposly.Application.Abstractions;

namespace Proposly.Application.Hr.Commands.DeleteWorkerNote;

public sealed record DeleteWorkerNoteCommand(Guid UserId, Guid NoteId) : ICommand;
