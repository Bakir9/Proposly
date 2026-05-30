using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTimeEntry;

public record DeleteTimeEntryCommand(Guid ProjectId, Guid TimeEntryId) : ICommand;
