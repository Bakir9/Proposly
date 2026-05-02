using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProjectNote;

public record UpdateProjectNoteCommand(Guid ProjectId, Guid NoteId, string Title, string Content) : ICommand;
