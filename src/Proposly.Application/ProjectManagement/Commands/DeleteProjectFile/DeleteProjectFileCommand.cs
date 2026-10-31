using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteProjectFile;

public sealed record DeleteProjectFileCommand(Guid ProjectId, Guid FileId) : ICommand;
