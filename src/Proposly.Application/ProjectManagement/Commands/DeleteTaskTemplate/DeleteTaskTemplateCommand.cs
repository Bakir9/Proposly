using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTaskTemplate;

public sealed record DeleteTaskTemplateCommand(Guid TemplateId) : ICommand;
