using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddProjectMember;

public record AddProjectMemberCommand(Guid ProjectId, Guid UserId, string Name, string Role, decimal HourlyRate, string Currency) : ICommand<Guid>;
