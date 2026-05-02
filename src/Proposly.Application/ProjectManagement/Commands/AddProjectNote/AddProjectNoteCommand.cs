using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddProjectNote;

public record AddProjectNoteCommand(Guid ProjectId, string Title, string Content) : ICommand<Guid>;
