using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteProjectNote;

public record DeleteProjectNoteCommand(Guid ProjectId, Guid NoteId) : ICommand;
